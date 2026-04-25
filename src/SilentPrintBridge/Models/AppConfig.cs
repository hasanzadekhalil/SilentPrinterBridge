namespace SilentPrintBridge.Models;

public class AppConfig
{
    public ServerConfig Server { get; set; } = new();
    public PrinterConfig Printer { get; set; } = new();
    public Dictionary<string, PrinterProfile> Profiles { get; set; } = new();
    public LoggingConfig Logging { get; set; } = new();
    public PdfConfig Pdf { get; set; } = new();
}

public class ServerConfig
{
    public string Host { get; set; } = "127.0.0.1";
    public int Port { get; set; } = 17878;
    public bool RequireApiKey { get; set; } = false;
    public string ApiKey { get; set; } = "";
    public List<string> AllowedOrigins { get; set; } = new();
    public int MaxPayloadBytes { get; set; } = 1048576;
    public bool AllowRemoteConnections { get; set; } = false;
}

public class PrinterConfig
{
    public string PrinterName { get; set; } = "";
    public bool AllowPrinterOverride { get; set; } = true;
    public bool AllowDefaultPrinterFallback { get; set; } = false;
    public List<string> AllowedPrinters { get; set; } = new();
    public string DefaultMode { get; set; } = "escpos";
    public int ReceiptWidthMm { get; set; } = 58;
    public int CharsPerLine58mm { get; set; } = 32;
    public int CharsPerLine80mm { get; set; } = 48;
    public bool AppendCutCommand { get; set; } = true;
    public int AppendFeedBeforeCutLines { get; set; } = 4;
    public bool OpenDrawerEnabled { get; set; } = false;
    public string Encoding { get; set; } = "CP437";
    public bool DryRun { get; set; } = false;
    public bool EnablePayloadDump { get; set; } = false;
}

public class PrinterProfile
{
    public string PrinterName { get; set; } = "";
    public int ReceiptWidthMm { get; set; } = 58;
    public int CharsPerLine { get; set; } = 32;
    public bool AppendCutCommand { get; set; } = true;
}

public class LoggingConfig
{
    public string LogLevel { get; set; } = "Information";
    public bool LogToFile { get; set; } = true;
    public string LogDirectory { get; set; } = "C:\\ProgramData\\SilentPrintBridge\\logs";
}

public class PdfConfig
{
    public bool Enabled { get; set; } = false;
    public string Renderer { get; set; } = "disabled";
    public string RendererPath { get; set; } = "";
    public string TempDirectory { get; set; } = "C:\\ProgramData\\SilentPrintBridge\\temp";
}
