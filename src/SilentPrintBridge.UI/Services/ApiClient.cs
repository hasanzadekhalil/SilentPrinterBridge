using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.IO;
using System.Linq;

namespace SilentPrintBridge.UI.Services;

public class ApiClient
{
    private readonly HttpClient _httpClient;
    private string _baseUrl = "http://127.0.0.1:17878";

    public ApiClient()
    {
        _httpClient = new HttpClient();
        _httpClient.Timeout = TimeSpan.FromSeconds(10);
    }

    public void SetBaseUrl(string host, int port)
    {
        _baseUrl = $"http://{host}:{port}";
    }

    public async Task<(bool success, string message, string? errorCode)> CheckHealthAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/health");
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                return (true, "Service is healthy", null);
            }
            return (false, $"Health check failed: {response.StatusCode}", "HTTP_ERROR");
        }
        catch (HttpRequestException ex)
        {
            return (false, $"Connection failed: {ex.Message}", "CONNECTION_REFUSED");
        }
        catch (TaskCanceledException)
        {
            return (false, "Request timeout", "TIMEOUT");
        }
        catch (Exception ex)
        {
            return (false, $"Error: {ex.Message}", "UNKNOWN_ERROR");
        }
    }

    public async Task<string?> GetVersionAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/version");
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                var data = JsonSerializer.Deserialize<JsonElement>(json);
                return data.GetProperty("version").GetString();
            }
        }
        catch { }
        return null;
    }

    public async Task<(bool success, string message, string? errorCode)> TestPrintAsync(string? printerName = null)
    {
        try
        {
            var request = new { PrinterName = printerName };
            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync($"{_baseUrl}/test-print", content);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                var result = JsonSerializer.Deserialize<JsonElement>(responseBody);
                var message = result.GetProperty("message").GetString() ?? "Test print sent";

                // Try to open PDF if it's Microsoft Print to PDF
                if (printerName != null && printerName.Contains("Microsoft Print to PDF", StringComparison.OrdinalIgnoreCase))
                {
                    TryOpenLatestPdf();
                }

                return (true, message, null);
            }
            else
            {
                try
                {
                    var error = JsonSerializer.Deserialize<JsonElement>(responseBody);
                    var errorMsg = error.GetProperty("error").GetString() ?? "Unknown error";
                    var errorCode = error.TryGetProperty("errorCode", out var code)
                        ? code.GetString()
                        : "PRINT_FAILED";
                    return (false, errorMsg, errorCode);
                }
                catch
                {
                    return (false, $"HTTP {response.StatusCode}: {responseBody}", "HTTP_ERROR");
                }
            }
        }
        catch (HttpRequestException ex)
        {
            return (false, $"Connection failed: {ex.Message}", "CONNECTION_REFUSED");
        }
        catch (TaskCanceledException)
        {
            return (false, "Request timeout", "TIMEOUT");
        }
        catch (Exception ex)
        {
            return (false, $"Error: {ex.Message}", "UNKNOWN_ERROR");
        }
    }

    private void TryOpenLatestPdf()
    {
        try
        {
            // Check multiple common locations for PDF files
            var searchPaths = new[]
            {
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads"),
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)
            };

            FileInfo? latestPdf = null;
            DateTime latestTime = DateTime.MinValue;

            foreach (var searchPath in searchPaths)
            {
                if (!Directory.Exists(searchPath))
                    continue;

                var pdfFiles = Directory.GetFiles(searchPath, "*.pdf")
                    .Select(f => new FileInfo(f))
                    .Where(f => (DateTime.Now - f.LastWriteTime).TotalSeconds < 10)
                    .OrderByDescending(f => f.LastWriteTime);

                var newest = pdfFiles.FirstOrDefault();
                if (newest != null && newest.LastWriteTime > latestTime)
                {
                    latestTime = newest.LastWriteTime;
                    latestPdf = newest;
                }
            }

            if (latestPdf != null)
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = latestPdf.FullName,
                    UseShellExecute = true
                });
            }
        }
        catch
        {
            // Ignore errors opening PDF
        }
    }

    public async Task<bool> ReloadConfigAsync()
    {
        try
        {
            var response = await _httpClient.PostAsync($"{_baseUrl}/config/reload", null);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public async Task<(bool success, List<string> printers, string? error)> GetPrintersAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/printers");
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                var data = JsonSerializer.Deserialize<JsonElement>(json);
                var printers = new List<string>();

                foreach (var printer in data.GetProperty("printers").EnumerateArray())
                {
                    var name = printer.GetProperty("name").GetString();
                    if (name != null)
                        printers.Add(name);
                }

                return (true, printers, null);
            }
            return (false, new List<string>(), $"HTTP {response.StatusCode}");
        }
        catch (Exception ex)
        {
            return (false, new List<string>(), ex.Message);
        }
    }
}
