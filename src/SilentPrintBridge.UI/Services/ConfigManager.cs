using System.IO;
using System.Text.Json;
using SilentPrintBridge.UI.Models;

namespace SilentPrintBridge.UI.Services;

public class ConfigManager
{
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
            var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions
            {
                WriteIndented = true
            });
            File.WriteAllText(_configPath, json);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
