namespace SilentPrintBridge.Models;

public class PrinterInfo
{
    public string Name { get; set; } = "";
    public bool IsDefault { get; set; }
    public string? Status { get; set; }
}
