using Serilog;
using SilentPrintBridge.Branding;
using SilentPrintBridge.Services;
using SilentPrintBridge.Security;
using SilentPrintBridge.Models;
using SilentPrintBridge.Utils;

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
var runtimeConfigPath = AppPaths.ResolveRuntimeConfigPath(AppContext.BaseDirectory);
AppPaths.EnsureRuntimeConfigExists(AppContext.BaseDirectory);
AppPaths.NormalizeRuntimeConfig(AppContext.BaseDirectory);
builder.Configuration.AddJsonFile(runtimeConfigPath, optional: false, reloadOnChange: false);

// Configure Serilog
var logDirectory = builder.Configuration["Logging:LogDirectory"] ?? AppPaths.DefaultLogDirectory;
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
        if (!config.Server.AllowRemoteConnections)
        {
            policy.AllowAnyOrigin()
                  .AllowAnyMethod()
                  .AllowAnyHeader();
            return;
        }

        var origins = config.Server.AllowedOrigins
            .Where(origin => !string.IsNullOrWhiteSpace(origin))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (origins.Count == 0 || origins.Contains("*"))
        {
            policy.AllowAnyOrigin()
                  .AllowAnyMethod()
                  .AllowAnyHeader();
            return;
        }

        var allowsFileOrigin = origins.RemoveAll(origin => string.Equals(origin, "file://", StringComparison.OrdinalIgnoreCase)) > 0;

        if (allowsFileOrigin)
        {
            policy.SetIsOriginAllowed(origin =>
                string.Equals(origin, "null", StringComparison.OrdinalIgnoreCase) ||
                origins.Contains(origin, StringComparer.OrdinalIgnoreCase));
            policy.AllowAnyMethod()
                  .AllowAnyHeader();
            return;
        }

        policy.WithOrigins(origins.ToArray())
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
Log.Information("Using runtime config path: {ConfigPath}", runtimeConfigPath);

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

        bool isPdfPrinter = PdfPrinterHelper.IsPdfPrinter(printerName);
        bool isThermalPrinter = printerName.Contains("TM-", StringComparison.OrdinalIgnoreCase) ||
                               printerName.Contains("EPSON", StringComparison.OrdinalIgnoreCase) ||
                               printerName.Contains("STAR", StringComparison.OrdinalIgnoreCase) ||
                               printerName.Contains("POS", StringComparison.OrdinalIgnoreCase) ||
                               printerName.Contains("RECEIPT", StringComparison.OrdinalIgnoreCase);

        if (isPdfPrinter || !isThermalPrinter)
        {
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
" + BuildBranding.TestReceiptFooter;

            bool success = PdfPrinterHelper.PrintTextToPrinter(printerName, textContent, "Test Receipt");

            if (success)
            {
                return Results.Ok(new PrintResponse
                {
                    Success = true,
                    JobId = Guid.NewGuid().ToString("N"),
                    PrinterName = printerName,
                    Mode = "text",
                    Message = isPdfPrinter
                        ? "Test receipt printed successfully to PDF printer"
                        : "Test receipt printed successfully via Windows printer driver"
                });
            }

            return Results.BadRequest(new PrintResponse
            {
                Success = false,
                PrinterName = printerName,
                Error = "Failed to print test receipt via Windows printer driver",
                ErrorCode = "PRINT_FAILED"
            });
        }

        // Thermal ESC/POS printer path

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
