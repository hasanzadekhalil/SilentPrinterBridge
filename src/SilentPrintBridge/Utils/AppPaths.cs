using System.Text.Json;
using System.Text.Json.Nodes;

namespace SilentPrintBridge.Utils;

public static class AppPaths
{
    private const string ProductFolderName = "SilentPrintBridge";
    private static readonly string[] DefaultAllowedOrigins =
    {
        "http://localhost",
        "http://127.0.0.1",
        "file://"
    };

    public static string SharedDataDirectory =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), ProductFolderName);

    public static string SharedConfigPath => Path.Combine(SharedDataDirectory, "appsettings.json");

    public static string GetLocalConfigPath(string baseDirectory) =>
        Path.Combine(baseDirectory, "appsettings.json");

    public static string DefaultLogDirectory => Path.Combine(SharedDataDirectory, "logs");

    public static string DefaultTempDirectory => Path.Combine(SharedDataDirectory, "temp");

    public static string ResolveRuntimeConfigPath(string baseDirectory)
    {
        var localConfigPath = GetLocalConfigPath(baseDirectory);

        if (ShouldUseSharedConfig(baseDirectory, localConfigPath))
        {
            return SharedConfigPath;
        }

        return localConfigPath;
    }

    public static void EnsureRuntimeConfigExists(string baseDirectory)
    {
        var localConfigPath = GetLocalConfigPath(baseDirectory);
        var runtimeConfigPath = ResolveRuntimeConfigPath(baseDirectory);

        if (runtimeConfigPath.Equals(SharedConfigPath, StringComparison.OrdinalIgnoreCase))
        {
            EnsureSharedConfigExists(localConfigPath);
            return;
        }

        if (!File.Exists(localConfigPath))
        {
            if (File.Exists(SharedConfigPath))
            {
                File.Copy(SharedConfigPath, localConfigPath, overwrite: false);
            }
            else
            {
                File.WriteAllText(localConfigPath, CreateDefaultConfigJson());
            }
        }
    }

    public static void NormalizeRuntimeConfig(string baseDirectory)
    {
        var runtimeConfigPath = ResolveRuntimeConfigPath(baseDirectory);
        if (!File.Exists(runtimeConfigPath))
        {
            return;
        }

        try
        {
            var json = File.ReadAllText(runtimeConfigPath);
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement.Clone();

            if (!root.TryGetProperty("Server", out var serverElement) ||
                !serverElement.TryGetProperty("AllowedOrigins", out var allowedOriginsElement) ||
                allowedOriginsElement.ValueKind != JsonValueKind.Array)
            {
                return;
            }

            var origins = allowedOriginsElement.EnumerateArray()
                .Select(x => x.GetString())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .ToList();

            if (origins.Count == 1 && origins[0] == "*")
            {
                var rootNode = JsonSerializer.Deserialize<JsonObject>(json) ?? new JsonObject();
                var serverNode = rootNode["Server"] as JsonObject ?? new JsonObject();
                serverNode["AllowedOrigins"] = new JsonArray(
                    DefaultAllowedOrigins
                        .Select(origin => (JsonNode?)JsonValue.Create(origin))
                        .ToArray());
                rootNode["Server"] = serverNode;
                File.WriteAllText(runtimeConfigPath, rootNode.ToJsonString(new JsonSerializerOptions
                {
                    WriteIndented = true
                }));
            }
        }
        catch
        {
        }
    }

    public static bool IsInstalledLocation(string baseDirectory)
    {
        var normalizedBaseDir = Path.GetFullPath(baseDirectory)
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles)
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var programFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86)
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

        return normalizedBaseDir.StartsWith(programFiles, StringComparison.OrdinalIgnoreCase) ||
               normalizedBaseDir.StartsWith(programFilesX86, StringComparison.OrdinalIgnoreCase);
    }

    private static bool ShouldUseSharedConfig(string baseDirectory, string localConfigPath)
    {
        if (IsInstalledLocation(baseDirectory))
        {
            return true;
        }

        if (File.Exists(localConfigPath))
        {
            return false;
        }

        try
        {
            Directory.CreateDirectory(baseDirectory);
            var testPath = Path.Combine(baseDirectory, $".write-test-{Guid.NewGuid():N}.tmp");
            File.WriteAllText(testPath, "test");
            File.Delete(testPath);
            return false;
        }
        catch
        {
            return true;
        }
    }

    public static void EnsureSharedConfigExists(string fallbackConfigPath)
    {
        EnsureSharedDirectories();

        if (File.Exists(SharedConfigPath))
        {
            return;
        }

        if (File.Exists(fallbackConfigPath))
        {
            File.Copy(fallbackConfigPath, SharedConfigPath, overwrite: false);
            return;
        }

        File.WriteAllText(SharedConfigPath, CreateDefaultConfigJson());
    }

    public static void EnsureSharedDirectories()
    {
        Directory.CreateDirectory(SharedDataDirectory);
        Directory.CreateDirectory(DefaultLogDirectory);
        Directory.CreateDirectory(DefaultTempDirectory);
    }

    public static string CreateDefaultConfigJson()
    {
        var config = new
        {
            Printer = new
            {
                PrinterName = "",
                ReceiptWidthMm = 80,
                AppendCutCommand = true,
                AppendFeedBeforeCutLines = 3
            },
            Server = new
            {
                Host = "127.0.0.1",
                Port = 17878,
                AllowRemoteConnections = false,
                RequireApiKey = false,
                ApiKey = "",
                AllowedOrigins = DefaultAllowedOrigins
            },
            Logging = new
            {
                LogDirectory = DefaultLogDirectory
            },
            Pdf = new
            {
                TempDirectory = DefaultTempDirectory
            }
        };

        return JsonSerializer.Serialize(config, new JsonSerializerOptions
        {
            WriteIndented = true
        });
    }
}
