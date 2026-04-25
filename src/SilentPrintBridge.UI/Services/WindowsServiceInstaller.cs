using System.ServiceProcess;
using System.Diagnostics;

namespace SilentPrintBridge.UI.Services;

public class WindowsServiceInstaller
{
    private readonly string _serviceName = "SilentPrintBridge";
    private readonly string _displayName = "SilentPrintBridge Service";
    private readonly string _description = "Silent browser-to-printer communication service for thermal printers";
    private readonly string _exePath;

    public WindowsServiceInstaller(string exePath)
    {
        _exePath = exePath;
    }

    public async Task<(bool success, string message)> InstallServiceAsync()
    {
        try
        {
            // Check if already installed
            if (IsServiceInstalled())
            {
                return (false, "Service is already installed. Uninstall it first.");
            }

            // Use sc.exe to create service
            var startInfo = new ProcessStartInfo
            {
                FileName = "sc.exe",
                Arguments = $"create {_serviceName} binPath= \"{_exePath}\" start= auto DisplayName= \"{_displayName}\"",
                UseShellExecute = true,
                Verb = "runas", // Request admin
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden
            };

            var process = Process.Start(startInfo);
            if (process == null)
            {
                return (false, "Failed to start installation process");
            }

            await Task.Run(() => process.WaitForExit());

            if (process.ExitCode != 0)
            {
                return (false, $"Installation failed with exit code {process.ExitCode}");
            }

            // Set description
            var descStartInfo = new ProcessStartInfo
            {
                FileName = "sc.exe",
                Arguments = $"description {_serviceName} \"{_description}\"",
                UseShellExecute = false,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden
            };

            var descProcess = Process.Start(descStartInfo);
            descProcess?.WaitForExit();

            return (true, "Service installed successfully. You can now start it from Services or use the UI.");
        }
        catch (System.ComponentModel.Win32Exception ex) when (ex.NativeErrorCode == 1223)
        {
            return (false, "Administrator permission denied");
        }
        catch (Exception ex)
        {
            return (false, $"Installation error: {ex.Message}");
        }
    }

    public async Task<(bool success, string message)> UninstallServiceAsync()
    {
        try
        {
            if (!IsServiceInstalled())
            {
                return (false, "Service is not installed");
            }

            // Stop service first if running
            if (IsServiceRunning())
            {
                var stopResult = await StopServiceAsync();
                if (!stopResult.success)
                {
                    return (false, $"Failed to stop service: {stopResult.message}");
                }
            }

            // Delete service
            var startInfo = new ProcessStartInfo
            {
                FileName = "sc.exe",
                Arguments = $"delete {_serviceName}",
                UseShellExecute = true,
                Verb = "runas",
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden
            };

            var process = Process.Start(startInfo);
            if (process == null)
            {
                return (false, "Failed to start uninstallation process");
            }

            await Task.Run(() => process.WaitForExit());

            if (process.ExitCode != 0)
            {
                return (false, $"Uninstallation failed with exit code {process.ExitCode}");
            }

            return (true, "Service uninstalled successfully");
        }
        catch (System.ComponentModel.Win32Exception ex) when (ex.NativeErrorCode == 1223)
        {
            return (false, "Administrator permission denied");
        }
        catch (Exception ex)
        {
            return (false, $"Uninstallation error: {ex.Message}");
        }
    }

    public async Task<(bool success, string message)> StartServiceAsync()
    {
        try
        {
            if (!IsServiceInstalled())
            {
                return (false, "Service is not installed");
            }

            if (IsServiceRunning())
            {
                return (false, "Service is already running");
            }

            var startInfo = new ProcessStartInfo
            {
                FileName = "sc.exe",
                Arguments = $"start {_serviceName}",
                UseShellExecute = true,
                Verb = "runas",
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden
            };

            var process = Process.Start(startInfo);
            if (process == null)
            {
                return (false, "Failed to start service");
            }

            await Task.Run(() => process.WaitForExit());

            if (process.ExitCode != 0)
            {
                return (false, $"Failed to start service (exit code {process.ExitCode})");
            }

            return (true, "Service started successfully");
        }
        catch (Exception ex)
        {
            return (false, $"Start error: {ex.Message}");
        }
    }

    public async Task<(bool success, string message)> StopServiceAsync()
    {
        try
        {
            if (!IsServiceInstalled())
            {
                return (false, "Service is not installed");
            }

            if (!IsServiceRunning())
            {
                return (false, "Service is not running");
            }

            var startInfo = new ProcessStartInfo
            {
                FileName = "sc.exe",
                Arguments = $"stop {_serviceName}",
                UseShellExecute = true,
                Verb = "runas",
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden
            };

            var process = Process.Start(startInfo);
            if (process == null)
            {
                return (false, "Failed to stop service");
            }

            await Task.Run(() => process.WaitForExit());

            if (process.ExitCode != 0)
            {
                return (false, $"Failed to stop service (exit code {process.ExitCode})");
            }

            return (true, "Service stopped successfully");
        }
        catch (Exception ex)
        {
            return (false, $"Stop error: {ex.Message}");
        }
    }

    public bool IsServiceInstalled()
    {
        try
        {
            using var controller = new ServiceController(_serviceName);
            var status = controller.Status;
            return true;
        }
        catch
        {
            return false;
        }
    }

    public bool IsServiceRunning()
    {
        try
        {
            using var controller = new ServiceController(_serviceName);
            return controller.Status == ServiceControllerStatus.Running;
        }
        catch
        {
            return false;
        }
    }

    public string GetServiceStatus()
    {
        try
        {
            using var controller = new ServiceController(_serviceName);
            return controller.Status.ToString();
        }
        catch
        {
            return "Not Installed";
        }
    }
}
