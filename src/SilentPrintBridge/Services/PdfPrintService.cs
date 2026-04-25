namespace SilentPrintBridge.Services;

public class PdfPrintService
{
    private readonly ILogger<PdfPrintService> _logger;
    private readonly Models.AppConfig _config;

    public PdfPrintService(ILogger<PdfPrintService> logger, Models.AppConfig config)
    {
        _logger = logger;
        _config = config;
    }

    public class PdfPrintResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = "";
        public string? ErrorCode { get; set; }
        public string? TempFilePath { get; set; }
    }

    public PdfPrintResult ProcessPdfBase64(string base64Data, string printerName, string jobName)
    {
        // Check if PDF is enabled
        if (!_config.Pdf.Enabled)
        {
            _logger.LogWarning("PDF printing requested but PDF support is disabled");
            return new PdfPrintResult
            {
                Success = false,
                ErrorCode = "PDF_DISABLED",
                Message = "PDF printing is disabled. ESC/POS and text modes are available. Enable a PDF renderer in config to use pdf_base64 mode."
            };
        }

        try
        {
            // Decode base64
            byte[] pdfBytes;
            try
            {
                pdfBytes = Convert.FromBase64String(base64Data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to decode base64 PDF data");
                return new PdfPrintResult
                {
                    Success = false,
                    ErrorCode = "INVALID_BASE64",
                    Message = "Invalid base64 encoding"
                };
            }

            // Validate PDF header
            if (pdfBytes.Length < 4 ||
                pdfBytes[0] != 0x25 || pdfBytes[1] != 0x50 ||
                pdfBytes[2] != 0x44 || pdfBytes[3] != 0x46) // %PDF
            {
                _logger.LogError("Invalid PDF header");
                return new PdfPrintResult
                {
                    Success = false,
                    ErrorCode = "INVALID_PDF",
                    Message = "Data does not appear to be a valid PDF file"
                };
            }

            // Create temp directory if needed
            if (!Directory.Exists(_config.Pdf.TempDirectory))
            {
                Directory.CreateDirectory(_config.Pdf.TempDirectory);
            }

            // Save to temp file with safe random name
            string tempFileName = $"pdf_{Guid.NewGuid():N}.pdf";
            string tempFilePath = Path.Combine(_config.Pdf.TempDirectory, tempFileName);

            File.WriteAllBytes(tempFilePath, pdfBytes);
            _logger.LogInformation("Saved PDF to temp file: {TempFile}", tempFilePath);

            // PDF rendering/printing would go here
            // For now, return a clear message that renderer is not implemented
            return new PdfPrintResult
            {
                Success = false,
                ErrorCode = "PDF_RENDERER_NOT_CONFIGURED",
                Message = "PDF file validated and saved, but no PDF renderer is configured. Configure Pdf.Renderer and Pdf.RendererPath in appsettings.json to enable PDF printing.",
                TempFilePath = tempFilePath
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing PDF");
            return new PdfPrintResult
            {
                Success = false,
                ErrorCode = "PDF_PROCESSING_ERROR",
                Message = $"PDF processing error: {ex.Message}"
            };
        }
    }

    public void CleanupTempFile(string? filePath)
    {
        if (string.IsNullOrEmpty(filePath))
            return;

        try
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
                _logger.LogInformation("Deleted temp file: {FilePath}", filePath);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to delete temp file: {FilePath}", filePath);
        }
    }
}
