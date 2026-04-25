using System.Drawing.Printing;

namespace SilentPrintBridge.UI.Services;

public class PrinterService
{
    public List<string> GetInstalledPrinters()
    {
        var printers = new List<string>();

        foreach (string printer in PrinterSettings.InstalledPrinters)
        {
            printers.Add(printer);
        }

        return printers;
    }

    public string? GetDefaultPrinter()
    {
        try
        {
            var settings = new PrinterSettings();
            return settings.PrinterName;
        }
        catch
        {
            return null;
        }
    }

    public bool PrinterExists(string printerName)
    {
        foreach (string printer in PrinterSettings.InstalledPrinters)
        {
            if (printer.Equals(printerName, StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }
}
