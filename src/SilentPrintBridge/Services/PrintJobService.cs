using SilentPrintBridge.Models;

namespace SilentPrintBridge.Services;

public class PrintJobService
{
    private readonly ILogger<PrintJobService> _logger;
    private readonly AppConfig _config;
    private readonly Win32RawPrinter _rawPrinter;
    private readonly PrinterDiscoveryService _printerDiscovery;
    private readonly TextReceiptFormatter _textFormatter;
    private readonly PdfPrintService _pdfService;

    public PrintJobService(
        ILogger<PrintJobService> logger,
        AppConfig config,
        Win32RawPrinter rawPrinter,
        PrinterDiscoveryService printerDiscovery,
        TextReceiptFormatter textFormatter,
        PdfPrintService pdfService)
    {
        _logger = logger;
        _config = config;
        _rawPrinter = rawPrinter;
        _printerDiscovery = printerDiscovery;
        _textFormatter = textFormatter;
        _pdfService = pdfService;
    }

    public PrintResponse ProcessPrintRequest(PrintRequest request)
    {
        var requestId = request.RequestId ?? Guid.NewGuid().ToString("N");
        var jobId = Guid.NewGuid().ToString("N");

        try
        {
            // Validate mode
            if (string.IsNullOrWhiteSpace(request.Mode))
            {
                return CreateErrorResponse(requestId, "INVALID_MODE", "Mode is required");
            }

            var mode = request.Mode.ToLower();
            if (mode != "escpos" && mode != "text" && mode != "pdf_base64")
            {
                return CreateErrorResponse(requestId, "INVALID_MODE", $"Invalid mode '{request.Mode}'. Supported: escpos, text, pdf_base64");
            }

            // Validate data
            if (string.IsNullOrWhiteSpace(request.Data))
            {
                return CreateErrorResponse(requestId, "INVALID_DATA", "Data is required");
            }

            // Validate payload size
            int dataSize = request.Data.Length;
            if (dataSize > _config.Server.MaxPayloadBytes)
            {
                return CreateErrorResponse(requestId, "PAYLOAD_TOO_LARGE",
                    $"Payload size {dataSize} exceeds maximum {_config.Server.MaxPayloadBytes} bytes");
            }

            // Validate copies
            if (request.Copies < 1 || request.Copies > 5)
            {
                return CreateErrorResponse(requestId, "INVALID_COPIES", "Copies must be between 1 and 5");
            }

            // Determine printer name
            string? printerName = DeterminePrinterName(request);
            if (string.IsNullOrWhiteSpace(printerName))
            {
                return CreateErrorResponse(requestId, "PRINTER_NOT_CONFIGURED",
                    "No printer configured. Set PrinterName in config or provide printerName in request");
            }

            // Validate printer exists
            if (!_printerDiscovery.PrinterExists(printerName))
            {
                return CreateErrorResponse(requestId, "PRINTER_NOT_FOUND",
                    $"Printer '{printerName}' not found. Use /printers endpoint to list available printers");
            }

            // Check allowlist
            if (_config.Printer.AllowedPrinters.Count > 0)
            {
                if (!_config.Printer.AllowedPrinters.Any(p => p.Equals(printerName, StringComparison.OrdinalIgnoreCase)))
                {
                    return CreateErrorResponse(requestId, "PRINTER_NOT_ALLOWED",
                        $"Printer '{printerName}' is not in the allowed printers list");
                }
            }

            // Dry run mode
            if (_config.Printer.DryRun)
            {
                _logger.LogInformation("DRY RUN: Would print to '{PrinterName}', mode={Mode}, copies={Copies}, jobName={JobName}",
                    printerName, mode, request.Copies, request.JobName);

                if (_config.Printer.EnablePayloadDump)
                {
                    _logger.LogInformation("DRY RUN: Payload length={Length}", request.Data.Length);
                }

                return new PrintResponse
                {
                    Success = true,
                    JobId = jobId,
                    RequestId = requestId,
                    PrinterName = printerName,
                    Mode = mode,
                    Message = "Dry run completed (no actual printing)"
                };
            }

            // Process based on mode
            byte[] printData;
            switch (mode)
            {
                case "escpos":
                    printData = ProcessEscPosMode(request);
                    break;

                case "text":
                    return ProcessTextMode(request, printerName, requestId, jobId);

                case "pdf_base64":
                    return ProcessPdfMode(request, printerName, requestId, jobId);

                default:
                    return CreateErrorResponse(requestId, "INVALID_MODE", $"Unsupported mode: {mode}");
            }

            // Print copies
            for (int copy = 1; copy <= request.Copies; copy++)
            {
                string copyJobName = request.Copies > 1 ? $"{request.JobName} (Copy {copy})" : request.JobName;

                var result = _rawPrinter.SendBytesToPrinter(printerName, printData, copyJobName);

                if (!result.Success)
                {
                    return CreateErrorResponse(requestId, "PRINT_FAILED",
                        $"Print failed on copy {copy}: {result.Message}", jobId, printerName, mode);
                }
            }

            return new PrintResponse
            {
                Success = true,
                JobId = jobId,
                RequestId = requestId,
                PrinterName = printerName,
                Mode = mode,
                Message = $"Successfully printed {request.Copies} cop{(request.Copies == 1 ? "y" : "ies")}"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing print request");
            return CreateErrorResponse(requestId, "INTERNAL_ERROR", $"Internal error: {ex.Message}", jobId);
        }
    }

    private string? DeterminePrinterName(PrintRequest request)
    {
        // Check if profile is specified
        if (!string.IsNullOrWhiteSpace(request.Profile))
        {
            if (_config.Profiles.TryGetValue(request.Profile, out var profile))
            {
                if (!string.IsNullOrWhiteSpace(profile.PrinterName))
                {
                    return profile.PrinterName;
                }
            }
        }

        // Check if printer override is provided and allowed
        if (!string.IsNullOrWhiteSpace(request.PrinterName))
        {
            if (_config.Printer.AllowPrinterOverride)
            {
                return request.PrinterName;
            }
            else
            {
                _logger.LogWarning("Printer override requested but not allowed by config");
            }
        }

        // Use configured printer
        if (!string.IsNullOrWhiteSpace(_config.Printer.PrinterName))
        {
            return _config.Printer.PrinterName;
        }

        // Fallback to default printer if allowed
        if (_config.Printer.AllowDefaultPrinterFallback)
        {
            return _printerDiscovery.GetDefaultPrinter();
        }

        return null;
    }

    private byte[] ProcessEscPosMode(PrintRequest request)
    {
        byte[] data;

        if (request.Encoding.ToLower() == "base64")
        {
            data = Convert.FromBase64String(request.Data);
        }
        else
        {
            // UTF-8 text to bytes
            var encoding = System.Text.Encoding.UTF8;
            data = encoding.GetBytes(request.Data);
        }

        // Optionally append cut command
        if (request.Cut && _config.Printer.AppendCutCommand)
        {
            var builder = new EscPosBuilder();
            builder.FeedLines(_config.Printer.AppendFeedBeforeCutLines);
            builder.FullCut();
            var cutBytes = builder.Build();

            var combined = new byte[data.Length + cutBytes.Length];
            Array.Copy(data, 0, combined, 0, data.Length);
            Array.Copy(cutBytes, 0, combined, data.Length, cutBytes.Length);
            return combined;
        }

        return data;
    }

    private PrintResponse ProcessTextMode(PrintRequest request, string printerName, string requestId, string jobId)
    {
        string text = _textFormatter.SanitizeText(request.Data);

        for (int copy = 1; copy <= request.Copies; copy++)
        {
            string copyJobName = request.Copies > 1 ? $"{request.JobName} (Copy {copy})" : request.JobName;

            if (PdfPrinterHelper.IsPdfPrinter(printerName))
            {
                bool pdfSuccess = PdfPrinterHelper.PrintTextToPrinter(printerName, text, copyJobName);
                if (!pdfSuccess)
                {
                    return CreateErrorResponse(requestId, "PRINT_FAILED",
                        $"Print failed on copy {copy}: Failed to print text via Windows printer driver", jobId, printerName, "text");
                }
            }
            else
            {
                int charsPerLine = request.ReceiptWidth switch
                {
                    80 => _config.Printer.CharsPerLine80mm,
                    58 => _config.Printer.CharsPerLine58mm,
                    _ => _config.Printer.CharsPerLine58mm
                };

                string encoding = request.CodePage ?? _config.Printer.Encoding;
                bool appendCut = request.Cut && _config.Printer.AppendCutCommand;
                int feedLines = appendCut ? _config.Printer.AppendFeedBeforeCutLines : 0;

                var printData = _textFormatter.FormatTextToEscPos(text, charsPerLine, appendCut, feedLines, encoding);
                var result = _rawPrinter.SendBytesToPrinter(printerName, printData, copyJobName);

                if (!result.Success)
                {
                    bool fallbackSuccess = PdfPrinterHelper.PrintTextToPrinter(printerName, text, copyJobName);
                    if (!fallbackSuccess)
                    {
                        return CreateErrorResponse(requestId, "PRINT_FAILED",
                            $"Print failed on copy {copy}: {result.Message}", jobId, printerName, "text");
                    }
                }
            }
        }

        return new PrintResponse
        {
            Success = true,
            JobId = jobId,
            RequestId = requestId,
            PrinterName = printerName,
            Mode = "text",
            Message = $"Successfully printed {request.Copies} cop{(request.Copies == 1 ? "y" : "ies")}"
        };
    }

    private PrintResponse ProcessPdfMode(PrintRequest request, string printerName, string requestId, string jobId)
    {
        var result = _pdfService.ProcessPdfBase64(request.Data, printerName, request.JobName);

        if (!result.Success)
        {
            // Cleanup temp file if created
            _pdfService.CleanupTempFile(result.TempFilePath);

            return CreateErrorResponse(requestId, result.ErrorCode ?? "PDF_ERROR", result.Message, jobId, printerName, "pdf_base64");
        }

        // If we get here, PDF was processed successfully (future implementation)
        _pdfService.CleanupTempFile(result.TempFilePath);

        return new PrintResponse
        {
            Success = true,
            JobId = jobId,
            RequestId = requestId,
            PrinterName = printerName,
            Mode = "pdf_base64",
            Message = result.Message
        };
    }

    private PrintResponse CreateErrorResponse(string requestId, string errorCode, string message,
        string? jobId = null, string? printerName = null, string? mode = null)
    {
        return new PrintResponse
        {
            Success = false,
            JobId = jobId,
            RequestId = requestId,
            PrinterName = printerName,
            Mode = mode,
            Error = message,
            ErrorCode = errorCode
        };
    }
}
