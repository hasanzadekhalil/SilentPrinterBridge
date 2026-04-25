namespace SilentPrintBridge.Models;

public class PrintRequest
{
    public string Mode { get; set; } = "escpos";
    public string? PrinterName { get; set; }
    public string? Profile { get; set; }
    public string Data { get; set; } = "";
    public string Encoding { get; set; } = "base64";
    public bool Cut { get; set; } = true;
    public bool OpenDrawer { get; set; } = false;
    public int Copies { get; set; } = 1;
    public string JobName { get; set; } = "Receipt";
    public int? ReceiptWidth { get; set; }
    public string? CodePage { get; set; }
    public string? RequestId { get; set; }
}
