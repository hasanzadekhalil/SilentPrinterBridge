using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Net.Http;

namespace SilentPrintBridge.UI.Services;

public class ServiceManager
{
    private Process? _serviceProcess;
    private readonly string _exePath;
    private readonly string _workingDirectory;
    private readonly HttpClient _httpClient;
    private System.Threading.Timer? _healthCheckTimer;

    public event EventHandler<string>? OutputReceived;
    public event EventHandler<bool>? StatusChanged;
    public event EventHandler<string>? ErrorOccurred;

    public bool IsRunning => _serviceProcess != null && !_serviceProcess.HasExited;
    public bool IsHealthy { get; private set; }

    public ServiceManager(string exePath, string workingDirectory)
    {
        _exePath = exePath;
        _workingDirectory = workingDirectory;
        _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
    }

    public async Task<(bool success, string message)> StartAsync()
    {
        if (IsRunning)
            return (false, "Service is already running");

        if (!File.Exists(_exePath))
            return (false, $"Service executable not found: {_exePath}");

        // Check if port is already in use and kill existing processes
        await KillExistingServiceProcessesAsync();

        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = _exePath,
                Arguments = "--console",
                WorkingDirectory = _workingDirectory,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            _serviceProcess = new Process { StartInfo = startInfo };

            _serviceProcess.OutputDataReceived += (s, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                    OutputReceived?.Invoke(this, e.Data);
            };

            _serviceProcess.ErrorDataReceived += (s, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    OutputReceived?.Invoke(this, $"ERROR: {e.Data}");
                    ErrorOccurred?.Invoke(this, e.Data);
                }
            };

            _serviceProcess.Exited += (s, e) =>
            {
                IsHealthy = false;
                StatusChanged?.Invoke(this, false);
                StopHealthCheck();
            };

            _serviceProcess.EnableRaisingEvents = true;
            _serviceProcess.Start();
            _serviceProcess.BeginOutputReadLine();
            _serviceProcess.BeginErrorReadLine();

            OutputReceived?.Invoke(this, "Service process started, waiting for API...");

            // Wait for API to be ready
            for (int i = 0; i < 30; i++)
            {
                await Task.Delay(1000);

                if (await CheckHealthAsync())
                {
                    IsHealthy = true;
                    StatusChanged?.Invoke(this, true);
                    StartHealthCheck();
                    OutputReceived?.Invoke(this, "✓ Service is ready and responding");
                    return (true, "Service started successfully");
                }

                if (_serviceProcess.HasExited)
                {
                    var exitCode = _serviceProcess.ExitCode;
                    return (false, $"Service exited unexpectedly with code {exitCode}. Check if port 17878 is available.");
                }
            }

            return (false, "Service started but API is not responding after 30 seconds");
        }
        catch (System.ComponentModel.Win32Exception ex) when (ex.NativeErrorCode == 1223)
        {
            return (false, "Administrator permission denied");
        }
        catch (Exception ex)
        {
            ErrorOccurred?.Invoke(this, ex.Message);
            return (false, $"Failed to start: {ex.Message}");
        }
    }

    private async Task KillExistingServiceProcessesAsync()
    {
        try
        {
            var processes = System.Diagnostics.Process.GetProcessesByName("SilentPrintBridge");
            foreach (var process in processes)
            {
                try
                {
                    if (process.Id != System.Diagnostics.Process.GetCurrentProcess().Id)
                    {
                        process.Kill(true);
                        await Task.Run(() => process.WaitForExit(2000));
                        process.Dispose();
                    }
                }
                catch { }
            }

            // Wait a bit for port to be released
            await Task.Delay(500);
        }
        catch { }
    }

    public async Task<bool> StopAsync()
    {
        if (!IsRunning)
            return false;

        try
        {
            StopHealthCheck();
            _serviceProcess?.Kill(true);
            await Task.Run(() => _serviceProcess?.WaitForExit(5000));
            _serviceProcess?.Dispose();
            _serviceProcess = null;

            IsHealthy = false;
            StatusChanged?.Invoke(this, false);
            return true;
        }
        catch (Exception ex)
        {
            ErrorOccurred?.Invoke(this, ex.Message);
            return false;
        }
    }

    public async Task<bool> CheckHealthAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("http://127.0.0.1:17878/health");
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    private void StartHealthCheck()
    {
        _healthCheckTimer = new System.Threading.Timer(async _ =>
        {
            var healthy = await CheckHealthAsync();
            if (IsHealthy != healthy)
            {
                IsHealthy = healthy;
                if (!healthy)
                {
                    ErrorOccurred?.Invoke(this, "Service health check failed");
                }
            }
        }, null, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(10));
    }

    private void StopHealthCheck()
    {
        _healthCheckTimer?.Dispose();
        _healthCheckTimer = null;
    }

    public void Dispose()
    {
        StopHealthCheck();
        _serviceProcess?.Dispose();
        _httpClient?.Dispose();
    }
}
