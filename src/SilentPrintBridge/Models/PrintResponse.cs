namespace SilentPrintBridge.Models;

public class PrintResponse
{
    public bool Success { get; set; }
    public string? JobId { get; set; }
    public string? RequestId { get; set; }
    public string? PrinterName { get; set; }
    public string? Mode { get; set; }
    public string? Message { get; set; }
    public string? Error { get; set; }
    public string? ErrorCode { get; set; }
}
