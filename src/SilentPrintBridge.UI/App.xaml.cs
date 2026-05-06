using System.Diagnostics;
using System.IO;
using System.Security.Principal;
using System.Windows;
using SilentPrintBridge.UI.Services;
using SilentPrintBridge.Utils;

namespace SilentPrintBridge.UI;

public partial class App : Application
{
    private SingleInstanceManager? _singleInstanceManager;

    protected override void OnStartup(StartupEventArgs e)
    {
        _singleInstanceManager = new SingleInstanceManager(
            @"Local\SilentPrintBridge.UI.SingleInstance",
            "SilentPrintBridge.UI.Activation");

        if (!_singleInstanceManager.IsPrimaryInstance)
        {
            _singleInstanceManager.SignalPrimaryInstanceAsync().GetAwaiter().GetResult();
            Shutdown();
            return;
        }

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

        if (!IsRunningAsAdministrator() && !AppPaths.IsInstalledLocation(baseDir))
        {
            var result = MessageBox.Show(
                "This application works best when started as Administrator.\n\nDo you want to restart it now with Administrator privileges?",
                "Administrator Permission Recommended",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                if (RestartAsAdministrator())
                {
                    Shutdown();
                    return;
                }
            }
        }

        try
        {
            AppPaths.EnsureRuntimeConfigExists(baseDir);
            AppPaths.NormalizeRuntimeConfig(baseDir);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Failed to initialize shared configuration storage.\n\n{ex.Message}",
                "Configuration Initialization Failed",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            Shutdown();
            return;
        }

        base.OnStartup(e);
        MainWindow = new MainWindow();
        _singleInstanceManager.ActivationRequested += OnActivationRequested;
        _singleInstanceManager.StartListening();
        MainWindow.Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _singleInstanceManager?.Dispose();
        base.OnExit(e);
    }

    private void OnActivationRequested()
    {
        Dispatcher.Invoke(() =>
        {
            if (MainWindow is MainWindow window)
            {
                window.RestoreFromExternalActivation();
            }
        });
    }

    private static bool IsRunningAsAdministrator()
    {
        using var identity = WindowsIdentity.GetCurrent();
        var principal = new WindowsPrincipal(identity);
        return principal.IsInRole(WindowsBuiltInRole.Administrator);
    }

    private static bool RestartAsAdministrator()
    {
        var exePath = Environment.ProcessPath;
        if (string.IsNullOrWhiteSpace(exePath))
        {
            return false;
        }

        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = exePath,
                UseShellExecute = true,
                Verb = "runas"
            };

            Process.Start(startInfo);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
