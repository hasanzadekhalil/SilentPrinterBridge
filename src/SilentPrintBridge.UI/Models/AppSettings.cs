namespace SilentPrintBridge.UI.Models;

public class AppSettings
{
    public PrinterSettings Printer { get; set; } = new();
    public ServerSettings Server { get; set; } = new();
    public LoggingSettings Logging { get; set; } = new();
}

public class PrinterSettings
{
    public string PrinterName { get; set; } = "";
    public int ReceiptWidthMm { get; set; } = 80;
    public bool AppendCutCommand { get; set; } = true;
    public int AppendFeedBeforeCutLines { get; set; } = 3;
}

public class ServerSettings
{
    public string Host { get; set; } = "127.0.0.1";
    public int Port { get; set; } = 17878;
    public bool AllowRemoteConnections { get; set; } = false;
    public bool RequireApiKey { get; set; } = false;
    public string ApiKey { get; set; } = "";
    public List<string> AllowedOrigins { get; set; } = new() { "*" };
}

public class LoggingSettings
{
    public string LogDirectory { get; set; } = "C:\\ProgramData\\SilentPrintBridge\\logs";
}
