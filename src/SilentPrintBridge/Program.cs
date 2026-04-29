using Serilog;
using SilentPrintBridge.Services;
using SilentPrintBridge.Security;
using SilentPrintBridge.Models;

// Parse command line arguments
var cmdArgs = Environment.GetCommandLineArgs();
bool consoleMode = cmdArgs.Contains("--console");
bool listPrinters = cmdArgs.Contains("--list-printers");
bool testPrint = cmdArgs.Contains("--test-print");
string? testPrinterName = null;

for (int i = 0; i < cmdArgs.Length - 1; i++)
{
    if (cmdArgs[i] == "--printer")
    {
        testPrinterName = cmdArgs[i + 1];
    }
}

var builder = WebApplication.CreateBuilder(Array.Empty<string>());

// Configure Serilog
var logDirectory = builder.Configuration["Logging:LogDirectory"] ?? "C:\\ProgramData\\SilentPrintBridge\\logs";
Directory.CreateDirectory(logDirectory);

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .WriteTo.File(
        Path.Combine(logDirectory, "silentprintbridge-.log"),
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 30)
    .CreateLogger();

builder.Host.UseSerilog();

// Configure Windows Service
if (!consoleMode && !listPrinters && !testPrint)
{
    builder.Host.UseWindowsService();
}

// Load configuration
var config = new AppConfig();
builder.Configuration.Bind(config);

// Register services
builder.Services.AddSingleton(config);
builder.Services.AddSingleton<ConfigService>();
builder.Services.AddSingleton<Win32RawPrinter>();
builder.Services.AddSingleton<PrinterDiscoveryService>();
builder.Services.AddSingleton<TextReceiptFormatter>();
builder.Services.AddSingleton<PdfPrintService>();
builder.Services.AddSingleton<PrintJobService>();

// Configure CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins(config.Server.AllowedOrigins.ToArray())
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Handle command line modes
if (listPrinters)
{
    var printerDiscovery = app.Services.GetRequiredService<PrinterDiscoveryService>();
    var printers = printerDiscovery.GetPrinterInfoList();

    Console.WriteLine("\nInstalled Printers:");
    Console.WriteLine("==================");
    foreach (var printer in printers)
    {
        Console.WriteLine($"{(printer.IsDefault ? "[DEFAULT] " : "          ")}{printer.Name}");
    }
    Console.WriteLine($"\nTotal: {printers.Length} printer(s)");
    return 0;
}

if (testPrint)
{
    var printerDiscovery = app.Services.GetRequiredService<PrinterDiscoveryService>();
    var rawPrinter = app.Services.GetRequiredService<Win32RawPrinter>();

    string printerName = testPrinterName ?? config.Printer.PrinterName;

    if (string.IsNullOrWhiteSpace(printerName))
    {
        Console.WriteLine("ERROR: No printer specified. Use --printer \"Printer Name\" or configure PrinterName in appsettings.json");
        return 1;
    }

    if (!printerDiscovery.PrinterExists(printerName))
    {
        Console.WriteLine($"ERROR: Printer '{printerName}' not found.");
        Console.WriteLine("\nAvailable printers:");
        foreach (var p in printerDiscovery.GetInstalledPrinters())
        {
            Console.WriteLine($"  - {p}");
        }
        return 1;
    }

    Console.WriteLine($"Sending test receipt to: {printerName}");

    var testData = EscPosBuilder.BuildTestReceipt("SilentPrintBridge", printerName, config.Printer.ReceiptWidthMm);

    // Add cut command if configured
    if (config.Printer.AppendCutCommand)
    {
        var escPosBuilder = new EscPosBuilder();
        escPosBuilder.FeedLines(config.Printer.AppendFeedBeforeCutLines);
        escPosBuilder.FullCut();
        var cutBytes = escPosBuilder.Build();

        var combined = new byte[testData.Length + cutBytes.Length];
        Array.Copy(testData, 0, combined, 0, testData.Length);
        Array.Copy(cutBytes, 0, combined, testData.Length, cutBytes.Length);
        testData = combined;
    }

    var result = rawPrinter.SendBytesToPrinter(printerName, testData, "Test Receipt");

    if (result.Success)
    {
        Console.WriteLine($"SUCCESS: Printed {result.BytesWritten} bytes");
        return 0;
    }
    else
    {
        Console.WriteLine($"ERROR: {result.Message}");
        return 1;
    }
}

// Configure middleware
app.UseCors();
app.UseMiddleware<ApiKeyMiddleware>();

// Configure Kestrel
var host = config.Server.AllowRemoteConnections ? "0.0.0.0" : config.Server.Host;
app.Urls.Clear();
app.Urls.Add($"http://{host}:{config.Server.Port}");

Log.Information("SilentPrintBridge starting on {Host}:{Port}", host, config.Server.Port);

// API Endpoints

app.MapGet("/health", (ConfigService configService, PrinterDiscoveryService printerDiscovery) =>
{
    var cfg = configService.GetConfig();
    var printers = printerDiscovery.GetInstalledPrinters();
    bool printerConfigured = !string.IsNullOrWhiteSpace(cfg.Printer.PrinterName);

    return Results.Ok(new
    {
        ok = true,
        service = "SilentPrintBridge",
        version = "1.0.0",
        printerConfigured,
        printerName = cfg.Printer.PrinterName,
        availablePrintersCount = printers.Count
    });
});

app.MapGet("/printers", (PrinterDiscoveryService printerDiscovery) =>
{
    var printers = printerDiscovery.GetPrinterInfoList();
    return Results.Ok(new { printers });
});

app.MapGet("/version", () =>
{
    return Results.Ok(new
    {
        service = "SilentPrintBridge",
        version = "1.0.0",
        platform = "Windows",
        runtime = Environment.Version.ToString()
    });
});

app.MapPost("/print", async (HttpContext context, PrintJobService printJobService) =>
{
    try
    {
        var request = await context.Request.ReadFromJsonAsync<PrintRequest>();

        if (request == null)
        {
            return Results.BadRequest(new PrintResponse
            {
                Success = false,
                Error = "Invalid request body",
                ErrorCode = "INVALID_REQUEST"
            });
        }

        var response = printJobService.ProcessPrintRequest(request);

        return response.Success ? Results.Ok(response) : Results.BadRequest(response);
    }
    catch (Exception ex)
    {
        Log.Error(ex, "Error in /print endpoint");
        return Results.BadRequest(new PrintResponse
        {
            Success = false,
            Error = $"Request processing error: {ex.Message}",
            ErrorCode = "REQUEST_ERROR"
        });
    }
});

app.MapPost("/test-print", async (HttpContext context, ConfigService configService,
    PrinterDiscoveryService printerDiscovery, Win32RawPrinter rawPrinter) =>
{
    try
    {
        var request = await context.Request.ReadFromJsonAsync<TestPrintRequest>();
        var cfg = configService.GetConfig();

        string printerName = request?.PrinterName ?? cfg.Printer.PrinterName;

        if (string.IsNullOrWhiteSpace(printerName))
        {
            return Results.BadRequest(new PrintResponse
            {
                Success = false,
                Error = "No printer configured",
                ErrorCode = "PRINTER_NOT_CONFIGURED"
            });
        }

        if (!printerDiscovery.PrinterExists(printerName))
        {
            return Results.BadRequest(new PrintResponse
            {
                Success = false,
                Error = $"Printer '{printerName}' not found",
                ErrorCode = "PRINTER_NOT_FOUND"
            });
        }

        var testData = EscPosBuilder.BuildTestReceipt("SilentPrintBridge", printerName, cfg.Printer.ReceiptWidthMm);

        // Check if it's a PDF printer
        bool isPdfPrinter = printerName.Contains("PDF", StringComparison.OrdinalIgnoreCase) ||
                           printerName.Contains("XPS", StringComparison.OrdinalIgnoreCase);

        if (isPdfPrinter)
        {
            // For PDF printers, use text-based printing instead of RAW ESC/POS
            var textContent = @"
========================================
    SILENTPRINTBRIDGE TEST RECEIPT
========================================

Service:        SilentPrintBridge
Version:        1.0.0
Printer:        " + printerName + @"
Date/Time:      " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + @"
Test ID:        " + Guid.NewGuid().ToString("N").Substring(0, 8) + @"

========================================
           TEST SUCCESSFUL
========================================

This is a test receipt to verify that
the printer is working correctly.

If you can read this, the printer is
configured and functioning properly.

========================================
     Developed by Khalil Hasanzade
     github.com/hasanzadekhalil
========================================
";

            bool success = PdfPrinterHelper.PrintTextToPrinter(printerName, textContent, "Test Receipt");

            if (success)
            {
                return Results.Ok(new PrintResponse
                {
                    Success = true,
                    JobId = Guid.NewGuid().ToString("N"),
                    PrinterName = printerName,
                    Mode = "text",
                    Message = $"Test receipt printed successfully to PDF printer"
                });
            }
            else
            {
                return Results.BadRequest(new PrintResponse
                {
                    Success = false,
                    PrinterName = printerName,
                    Error = "Failed to print to PDF printer",
                    ErrorCode = "PRINT_FAILED"
                });
            }
        }

        bool cut = request?.Cut ?? cfg.Printer.AppendCutCommand;
        if (cut)
        {
            var builder = new EscPosBuilder();
            builder.FeedLines(cfg.Printer.AppendFeedBeforeCutLines);
            builder.FullCut();
            var cutBytes = builder.Build();

            var combined = new byte[testData.Length + cutBytes.Length];
            Array.Copy(testData, 0, combined, 0, testData.Length);
            Array.Copy(cutBytes, 0, combined, testData.Length, cutBytes.Length);
            testData = combined;
        }

        var result = rawPrinter.SendBytesToPrinter(printerName, testData, "Test Receipt");

        if (result.Success)
        {
            return Results.Ok(new PrintResponse
            {
                Success = true,
                JobId = Guid.NewGuid().ToString("N"),
                PrinterName = printerName,
                Mode = "escpos",
                Message = $"Test receipt printed successfully ({result.BytesWritten} bytes)"
            });
        }
        else
        {
            return Results.BadRequest(new PrintResponse
            {
                Success = false,
                PrinterName = printerName,
                Error = result.Message,
                ErrorCode = "PRINT_FAILED"
            });
        }
    }
    catch (Exception ex)
    {
        Log.Error(ex, "Error in /test-print endpoint");
        return Results.BadRequest(new PrintResponse
        {
            Success = false,
            Error = $"Test print error: {ex.Message}",
            ErrorCode = "TEST_PRINT_ERROR"
        });
    }
});

app.MapPost("/config/reload", (ConfigService configService) =>
{
    try
    {
        configService.ReloadConfig();
        return Results.Ok(new
        {
            success = true,
            message = "Configuration reloaded successfully"
        });
    }
    catch (Exception ex)
    {
        Log.Error(ex, "Error reloading config");
        return Results.BadRequest(new
        {
            success = false,
            error = $"Failed to reload config: {ex.Message}"
        });
    }
});

Log.Information("SilentPrintBridge started successfully");
Log.Information("Configured printer: {PrinterName}", config.Printer.PrinterName);
Log.Information("API Key required: {Required}", config.Server.RequireApiKey);

app.Run();

return 0;

// Helper class for test-print request
public class TestPrintRequest
{
    public string? PrinterName { get; set; }
    public string? Profile { get; set; }
    public bool? Cut { get; set; }
}
