using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
using SilentPrintBridge.UI.Models;
using SilentPrintBridge.Utils;

namespace SilentPrintBridge.UI.Services;

public class ConfigManager
{
    private static readonly List<string> DefaultAllowedOrigins = new() { "http://localhost", "http://127.0.0.1", "file://" };
    private readonly string _configPath;

    public ConfigManager(string configPath)
    {
        _configPath = configPath;
    }

    public AppSettings LoadConfig()
    {
        try
        {
            if (!File.Exists(_configPath))
                return new AppSettings();

            var json = File.ReadAllText(_configPath);
            return JsonSerializer.Deserialize<AppSettings>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            }) ?? new AppSettings();
        }
        catch
        {
            return new AppSettings();
        }
    }

    public bool SaveConfig(AppSettings settings)
    {
        try
        {
            JsonObject root;
            if (File.Exists(_configPath))
            {
                root = JsonNode.Parse(File.ReadAllText(_configPath))?.AsObject() ?? new JsonObject();
            }
            else
            {
                root = JsonNode.Parse(CreateDefaultConfigJson())?.AsObject() ?? new JsonObject();
            }

            root["Printer"] = new JsonObject
            {
                ["PrinterName"] = settings.Printer.PrinterName,
                ["ReceiptWidthMm"] = settings.Printer.ReceiptWidthMm,
                ["AppendCutCommand"] = settings.Printer.AppendCutCommand,
                ["AppendFeedBeforeCutLines"] = settings.Printer.AppendFeedBeforeCutLines
            };

            root["Server"] = new JsonObject
            {
                ["Host"] = settings.Server.Host,
                ["Port"] = settings.Server.Port,
                ["AllowRemoteConnections"] = settings.Server.AllowRemoteConnections,
                ["RequireApiKey"] = settings.Server.RequireApiKey,
                ["ApiKey"] = settings.Server.ApiKey,
                ["AllowedOrigins"] = new JsonArray(settings.Server.AllowedOrigins.Select(origin => JsonValue.Create(origin)).ToArray())
            };

            var logging = root["Logging"]?.AsObject() ?? new JsonObject();
            logging["LogDirectory"] = string.IsNullOrWhiteSpace(settings.Logging.LogDirectory)
                ? AppPaths.DefaultLogDirectory
                : settings.Logging.LogDirectory;
            root["Logging"] = logging;

            File.WriteAllText(_configPath, root.ToJsonString(new JsonSerializerOptions
            {
                WriteIndented = true
            }));
            return true;
        }
        catch
        {
            return false;
        }
    }

    public string GetLogDirectory()
    {
        var config = LoadConfig();
        return string.IsNullOrWhiteSpace(config.Logging.LogDirectory)
            ? AppPaths.DefaultLogDirectory
            : config.Logging.LogDirectory;
    }

    public List<string> GetAllowedOrigins()
    {
        var config = LoadConfig();
        return config.Server.AllowedOrigins.Count > 0
            ? config.Server.AllowedOrigins
            : new List<string>(DefaultAllowedOrigins);
    }

    private static string CreateDefaultConfigJson()
    {
        return @"{
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
    ""AllowedOrigins"": [""http://localhost"", ""http://127.0.0.1"", ""file://""]
  },
  ""Logging"": {
    ""LogDirectory"": """ + AppPaths.DefaultLogDirectory.Replace("\\", "\\\\") + @"""
  }
}";
    }
}
