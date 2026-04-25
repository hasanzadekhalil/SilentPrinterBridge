using System.Drawing.Printing;

namespace SilentPrintBridge.Services;

public class PrinterDiscoveryService
{
    private readonly ILogger<PrinterDiscoveryService> _logger;

    public PrinterDiscoveryService(ILogger<PrinterDiscoveryService> logger)
    {
        _logger = logger;
    }

    public List<string> GetInstalledPrinters()
    {
        try
        {
            var printers = new List<string>();
            foreach (string printer in PrinterSettings.InstalledPrinters)
            {
                printers.Add(printer);
            }
            return printers;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to enumerate installed printers");
            return new List<string>();
        }
    }

    public string? GetDefaultPrinter()
    {
        try
        {
            var settings = new PrinterSettings();
            return settings.PrinterName;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get default printer");
            return null;
        }
    }

    public bool PrinterExists(string printerName)
    {
        if (string.IsNullOrWhiteSpace(printerName))
            return false;

        var printers = GetInstalledPrinters();
        return printers.Any(p => p.Equals(printerName, StringComparison.OrdinalIgnoreCase));
    }

    public Models.PrinterInfo[] GetPrinterInfoList()
    {
        var defaultPrinter = GetDefaultPrinter();
        var printers = GetInstalledPrinters();

        return printers.Select(p => new Models.PrinterInfo
        {
            Name = p,
            IsDefault = p.Equals(defaultPrinter, StringComparison.OrdinalIgnoreCase)
        }).ToArray();
    }
}
