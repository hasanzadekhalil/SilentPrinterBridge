using System.Configuration;
using System.Data;
using System.Windows;

namespace SilentPrintBridge.UI;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var baseDir = AppDomain.CurrentDomain.BaseDirectory;
        var exePath = System.IO.Path.Combine(baseDir, "SilentPrintBridge.exe");

        if (!System.IO.File.Exists(exePath))
        {
            MessageBox.Show(
                $"SilentPrintBridge.exe not found in:\n{baseDir}\n\nPlease ensure the service executable is in the same directory as this UI application.",
                "Service Executable Not Found",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            Shutdown();
            return;
        }

        var configPath = System.IO.Path.Combine(baseDir, "appsettings.json");
        if (!System.IO.File.Exists(configPath))
        {
            var defaultConfig = @"{
  ""Printer"": {
    ""PrinterName"": """",
    ""ReceiptWidthMm"": 80,
    ""AppendCutCommand"": true,
    ""AppendFeedBeforeCutLines"": 3
  },
  ""Server"": {
    ""Host"": ""127.0.0.1"",
    ""Port"": 17878,
    ""AllowRemoteConnections"": false,
    ""RequireApiKey"": false,
    ""ApiKey"": """",
    ""AllowedOrigins"": [""*""]
  },
  ""Logging"": {
    ""LogDirectory"": ""C:\\ProgramData\\SilentPrintBridge\\logs""
  }
}";
            try
            {
                System.IO.File.WriteAllText(configPath, defaultConfig);
            }
            catch { }
        }
    }
}

