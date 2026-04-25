using Microsoft.Extensions.Options;
using SilentPrintBridge.Models;

namespace SilentPrintBridge.Services;

public class ConfigService
{
    private readonly ILogger<ConfigService> _logger;
    private readonly IConfiguration _configuration;
    private AppConfig _config;

    public ConfigService(ILogger<ConfigService> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
        _config = LoadConfig();
    }

    public AppConfig GetConfig()
    {
        return _config;
    }

    public void ReloadConfig()
    {
        _logger.LogInformation("Reloading configuration from disk");
        _config = LoadConfig();
        _logger.LogInformation("Configuration reloaded successfully");
    }

    private AppConfig LoadConfig()
    {
        var config = new AppConfig();
        _configuration.Bind(config);
        return config;
    }
}
