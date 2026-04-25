using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Navigation;
using System.Diagnostics;
using Microsoft.Win32;
using SilentPrintBridge.UI.Models;
using SilentPrintBridge.UI.Services;
using Hardcodet.Wpf.TaskbarNotification;

namespace SilentPrintBridge.UI;

public partial class MainWindow : Window
{
    private readonly ServiceManager _serviceManager;
    private readonly ConfigManager _configManager;
    private readonly PrinterService _printerService;
    private readonly ApiClient _apiClient;
    private readonly string _exePath;
    private readonly string _configPath;
    private TaskbarIcon? _trayIcon;

    public MainWindow()
    {
        InitializeComponent();

        var baseDir = AppDomain.CurrentDomain.BaseDirectory;
        _exePath = Path.Combine(baseDir, "SilentPrintBridge.exe");
        _configPath = Path.Combine(baseDir, "appsettings.json");

        _serviceManager = new ServiceManager(_exePath, baseDir);
        _configManager = new ConfigManager(_configPath);
        _printerService = new PrinterService();
        _apiClient = new ApiClient();

        _serviceManager.OutputReceived += ServiceManager_OutputReceived;
        _serviceManager.StatusChanged += ServiceManager_StatusChanged;
        _serviceManager.ErrorOccurred += ServiceManager_ErrorOccurred;

        InitializeTrayIcon();
        LoadConfiguration();
        LoadPrinters();
        CheckAutoStart();
    }

    private void InitializeTrayIcon()
    {
        try
        {
            _trayIcon = new TaskbarIcon
            {
                ToolTipText = "SilentPrintBridge"
            };

            var contextMenu = new System.Windows.Controls.ContextMenu();

            var showItem = new System.Windows.Controls.MenuItem { Header = "Show" };
            showItem.Click += (s, e) => { Show(); WindowState = WindowState.Normal; };
            contextMenu.Items.Add(showItem);

            var startItem = new System.Windows.Controls.MenuItem { Header = "Start Service" };
            startItem.Click += async (s, e) => await StartServiceAsync();
            contextMenu.Items.Add(startItem);

            var stopItem = new System.Windows.Controls.MenuItem { Header = "Stop Service" };
            stopItem.Click += async (s, e) => await StopServiceAsync();
            contextMenu.Items.Add(stopItem);

            contextMenu.Items.Add(new System.Windows.Controls.Separator());

            var exitItem = new System.Windows.Controls.MenuItem { Header = "Exit" };
            exitItem.Click += async (s, e) => { await _serviceManager.StopAsync(); Application.Current.Shutdown(); };
            contextMenu.Items.Add(exitItem);

            _trayIcon.ContextMenu = contextMenu;
            _trayIcon.TrayLeftMouseDown += (s, e) => { Show(); WindowState = WindowState.Normal; };
        }
        catch
        {
            _trayIcon = null;
        }
    }

    private void LoadConfiguration()
    {
        var config = _configManager.LoadConfig();

        PrinterComboBox.Text = config.Printer.PrinterName;
        ReceiptWidthTextBox.Text = config.Printer.ReceiptWidthMm.ToString();
        AutoCutCheckBox.IsChecked = config.Printer.AppendCutCommand;
        FeedLinesTextBox.Text = config.Printer.AppendFeedBeforeCutLines.ToString();

        HostTextBox.Text = config.Server.Host;
        PortTextBox.Text = config.Server.Port.ToString();
        RemoteAccessCheckBox.IsChecked = config.Server.AllowRemoteConnections;
        RequireApiKeyCheckBox.IsChecked = config.Server.RequireApiKey;
        ApiKeyTextBox.Text = config.Server.ApiKey;
        ApiKeyTextBox.IsEnabled = config.Server.RequireApiKey;

        UpdateApiUrl();
    }

    private void LoadPrinters()
    {
        var printers = _printerService.GetInstalledPrinters();
        PrinterComboBox.ItemsSource = printers;

        if (printers.Count > 0 && string.IsNullOrEmpty(PrinterComboBox.Text))
        {
            var defaultPrinter = _printerService.GetDefaultPrinter();
            if (defaultPrinter != null)
                PrinterComboBox.Text = defaultPrinter;
        }
    }

    private void CheckAutoStart()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", false);
            var value = key?.GetValue("SilentPrintBridge");
            AutoStartCheckBox.IsChecked = value != null;
        }
        catch { }
    }

    private async void StartButton_Click(object sender, RoutedEventArgs e)
    {
        await StartServiceAsync();
    }

    private async void StopButton_Click(object sender, RoutedEventArgs e)
    {
        await StopServiceAsync();
    }

    private async void RestartButton_Click(object sender, RoutedEventArgs e)
    {
        StatusBarText.Text = "Restarting service...";
        await StopServiceAsync();
        await Task.Delay(2000);
        await StartServiceAsync();
    }

    private async void TestPrintButton_Click(object sender, RoutedEventArgs e)
    {
        var printerName = PrinterComboBox.Text;
        if (string.IsNullOrWhiteSpace(printerName))
        {
            ShowError("No Printer Selected", "Please select a printer first.");
            return;
        }

        StatusBarText.Text = "Sending test print...";
        LogTextBox.AppendText($"[{DateTime.Now:HH:mm:ss}] Sending test print to: {printerName}\n");

        var (success, message, errorCode) = await _apiClient.TestPrintAsync(printerName);

        if (success)
        {
            StatusBarText.Text = "Test print sent successfully!";
            LogTextBox.AppendText($"[{DateTime.Now:HH:mm:ss}] ✓ {message}\n");
            DebugTextBox.AppendText($"[SUCCESS] Test print completed\n");
            DebugTextBox.AppendText($"Printer: {printerName}\n");
            DebugTextBox.AppendText($"Message: {message}\n\n");
            MessageBox.Show(message, "Success", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        else
        {
            StatusBarText.Text = "Test print failed.";
            LogTextBox.AppendText($"[{DateTime.Now:HH:mm:ss}] ✗ ERROR: {message}\n");
            DebugTextBox.AppendText($"[ERROR] Test print failed\n");
            DebugTextBox.AppendText($"Error Code: {errorCode}\n");
            DebugTextBox.AppendText($"Message: {message}\n");
            DebugTextBox.AppendText($"Printer: {printerName}\n\n");
            ShowError($"Test Print Failed ({errorCode})", message);
        }

        LogTextBox.ScrollToEnd();
        DebugTextBox.ScrollToEnd();
    }

    private async void InstallServiceButton_Click(object sender, RoutedEventArgs e)
    {
        var installer = new WindowsServiceInstaller(_exePath);

        // Check if already installed
        if (installer.IsServiceInstalled())
        {
            var uninstallResult = MessageBox.Show(
                "Service is already installed.\n\n" +
                $"Current Status: {installer.GetServiceStatus()}\n\n" +
                "Do you want to uninstall it?",
                "Service Already Installed",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (uninstallResult == MessageBoxResult.Yes)
            {
                StatusBarText.Text = "Uninstalling service...";
                var (success, message) = await installer.UninstallServiceAsync();

                if (success)
                {
                    MessageBox.Show(message, "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    StatusBarText.Text = "Service uninstalled";
                }
                else
                {
                    ShowError("Uninstall Failed", message);
                    StatusBarText.Text = "Uninstall failed";
                }
            }
            return;
        }

        // Install service
        var result = MessageBox.Show(
            "This will install SilentPrintBridge as a Windows Service.\n\n" +
            "The service will:\n" +
            "• Start automatically with Windows\n" +
            "• Run in the background\n" +
            "• Require administrator privileges\n\n" +
            "Do you want to continue?",
            "Install Windows Service",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result == MessageBoxResult.Yes)
        {
            StatusBarText.Text = "Installing service...";
            LogTextBox.AppendText($"[{DateTime.Now:HH:mm:ss}] Installing Windows Service...\n");

            var (success, message) = await installer.InstallServiceAsync();

            if (success)
            {
                LogTextBox.AppendText($"[{DateTime.Now:HH:mm:ss}] ✓ {message}\n");
                MessageBox.Show(
                    $"{message}\n\n" +
                    "You can now:\n" +
                    "• Start the service from Windows Services\n" +
                    "• Or use this UI to manage it\n\n" +
                    "The service will start automatically on system boot.",
                    "Installation Successful",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                StatusBarText.Text = "Service installed successfully";
            }
            else
            {
                LogTextBox.AppendText($"[{DateTime.Now:HH:mm:ss}] ✗ ERROR: {message}\n");
                ShowError("Installation Failed", message);
                StatusBarText.Text = "Installation failed";
            }

            LogTextBox.ScrollToEnd();
        }
    }

    private void SaveConfigButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var config = new AppSettings
            {
                Printer = new PrinterSettings
                {
                    PrinterName = PrinterComboBox.Text,
                    ReceiptWidthMm = int.TryParse(ReceiptWidthTextBox.Text, out var width) ? width : 80,
                    AppendCutCommand = AutoCutCheckBox.IsChecked ?? true,
                    AppendFeedBeforeCutLines = int.TryParse(FeedLinesTextBox.Text, out var feed) ? feed : 3
                },
                Server = new ServerSettings
                {
                    Host = HostTextBox.Text,
                    Port = int.TryParse(PortTextBox.Text, out var port) ? port : 17878,
                    AllowRemoteConnections = RemoteAccessCheckBox.IsChecked ?? false,
                    RequireApiKey = RequireApiKeyCheckBox.IsChecked ?? false,
                    ApiKey = ApiKeyTextBox.Text,
                    AllowedOrigins = new List<string> { "*" }
                }
            };

            if (_configManager.SaveConfig(config))
            {
                StatusBarText.Text = "Configuration saved successfully!";
                UpdateApiUrl();

                if (_serviceManager.IsRunning)
                {
                    MessageBox.Show(
                        "Configuration saved.\n\nPlease restart the service for changes to take effect.",
                        "Configuration Saved",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show("Configuration saved successfully!", "Success",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            else
            {
                ShowError("Save Failed", "Failed to save configuration.");
            }
        }
        catch (Exception ex)
        {
            ShowError("Save Error", $"Error saving configuration: {ex.Message}");
        }
    }

    private void ClearLogsButton_Click(object sender, RoutedEventArgs e)
    {
        LogTextBox.Clear();
    }

    private void RefreshLogsButton_Click(object sender, RoutedEventArgs e)
    {
        StatusBarText.Text = "Logs refreshed";
    }

    private async void CheckHealthButton_Click(object sender, RoutedEventArgs e)
    {
        DebugTextBox.AppendText($"[{DateTime.Now:HH:mm:ss}] Checking service health...\n");

        var (success, message, errorCode) = await _apiClient.CheckHealthAsync();

        if (success)
        {
            DebugTextBox.AppendText($"✓ Service is healthy\n");
            DebugTextBox.AppendText($"Message: {message}\n\n");
        }
        else
        {
            DebugTextBox.AppendText($"✗ Health check failed\n");
            DebugTextBox.AppendText($"Error Code: {errorCode}\n");
            DebugTextBox.AppendText($"Message: {message}\n\n");
        }

        DebugTextBox.ScrollToEnd();
    }

    private async void ListPrintersButton_Click(object sender, RoutedEventArgs e)
    {
        DebugTextBox.AppendText($"[{DateTime.Now:HH:mm:ss}] Listing printers...\n");

        var (success, printers, error) = await _apiClient.GetPrintersAsync();

        if (success)
        {
            DebugTextBox.AppendText($"✓ Found {printers.Count} printer(s):\n");
            foreach (var printer in printers)
            {
                DebugTextBox.AppendText($"  - {printer}\n");
            }
            DebugTextBox.AppendText("\n");
        }
        else
        {
            DebugTextBox.AppendText($"✗ Failed to list printers\n");
            DebugTextBox.AppendText($"Error: {error}\n\n");
        }

        DebugTextBox.ScrollToEnd();
    }

    private async void TestApiButton_Click(object sender, RoutedEventArgs e)
    {
        DebugTextBox.AppendText($"[{DateTime.Now:HH:mm:ss}] Testing API connection...\n");

        var (success, message, errorCode) = await _apiClient.CheckHealthAsync();

        if (success)
        {
            DebugTextBox.AppendText($"✓ API is responding\n");
            DebugTextBox.AppendText($"Endpoint: http://127.0.0.1:17878/health\n");
            DebugTextBox.AppendText($"Status: OK\n\n");
        }
        else
        {
            DebugTextBox.AppendText($"✗ API connection failed\n");
            DebugTextBox.AppendText($"Error Code: {errorCode}\n");
            DebugTextBox.AppendText($"Message: {message}\n");
            DebugTextBox.AppendText($"Possible causes:\n");
            DebugTextBox.AppendText($"  - Service is not running\n");
            DebugTextBox.AppendText($"  - Port 17878 is blocked\n");
            DebugTextBox.AppendText($"  - Firewall is blocking connection\n\n");
        }

        DebugTextBox.ScrollToEnd();
    }

    private void OpenLogFolderButton_Click(object sender, RoutedEventArgs e)
    {
        var logDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data", "logs");

        if (!Directory.Exists(logDir))
        {
            Directory.CreateDirectory(logDir);
        }

        Process.Start("explorer.exe", logDir);
        DebugTextBox.AppendText($"[{DateTime.Now:HH:mm:ss}] Opened log folder: {logDir}\n\n");
        DebugTextBox.ScrollToEnd();
    }

    private void PrinterComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        StatusBarText.Text = $"Printer selected: {PrinterComboBox.Text}";
    }

    private void RequireApiKeyCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        ApiKeyTextBox.IsEnabled = RequireApiKeyCheckBox.IsChecked ?? false;
    }

    private void AutoStartCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true);
            if (key != null)
            {
                if (AutoStartCheckBox.IsChecked == true)
                {
                    var exePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "SilentPrintBridge.UI.exe");
                    key.SetValue("SilentPrintBridge", $"\"{exePath}\"");
                    StatusBarText.Text = "Auto-start enabled";
                }
                else
                {
                    key.DeleteValue("SilentPrintBridge", false);
                    StatusBarText.Text = "Auto-start disabled";
                }
            }
        }
        catch (Exception ex)
        {
            ShowError("Auto-start Error", $"Failed to update auto-start: {ex.Message}");
        }
    }

    private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
    {
        Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
        e.Handled = true;
    }

    private void Window_StateChanged(object sender, EventArgs e)
    {
        if (WindowState == WindowState.Minimized && MinimizeToTrayCheckBox.IsChecked == true)
        {
            Hide();
        }
    }

    private async void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
    {
        if (_serviceManager.IsRunning)
        {
            var result = MessageBox.Show(
                "Service is still running. Stop service and exit?",
                "Service Running",
                MessageBoxButton.YesNoCancel,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                await _serviceManager.StopAsync();
            }
            else if (result == MessageBoxResult.No)
            {
                e.Cancel = true;
                Hide();
                return;
            }
            else
            {
                e.Cancel = true;
                return;
            }
        }

        _trayIcon?.Dispose();
    }

    private async Task StartServiceAsync()
    {
        StartButton.IsEnabled = false;
        StatusBarText.Text = "Starting service...";
        LogTextBox.AppendText($"[{DateTime.Now:HH:mm:ss}] Starting service...\n");

        var (success, message) = await _serviceManager.StartAsync();

        if (success)
        {
            LogTextBox.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}\n");
        }
        else
        {
            LogTextBox.AppendText($"[{DateTime.Now:HH:mm:ss}] ERROR: {message}\n");
            ShowError("Service Start Failed", message);
            StartButton.IsEnabled = true;
        }

        LogTextBox.ScrollToEnd();
    }

    private async Task StopServiceAsync()
    {
        StatusBarText.Text = "Stopping service...";
        LogTextBox.AppendText($"[{DateTime.Now:HH:mm:ss}] Stopping service...\n");

        await _serviceManager.StopAsync();

        LogTextBox.AppendText($"[{DateTime.Now:HH:mm:ss}] Service stopped\n");
        LogTextBox.ScrollToEnd();
    }

    private void ServiceManager_OutputReceived(object? sender, string e)
    {
        Dispatcher.Invoke(() =>
        {
            LogTextBox.AppendText($"{e}\n");
            LogTextBox.ScrollToEnd();
        });
    }

    private void ServiceManager_StatusChanged(object? sender, bool isRunning)
    {
        Dispatcher.Invoke(() =>
        {
            StartButton.IsEnabled = !isRunning;
            StopButton.IsEnabled = isRunning;
            RestartButton.IsEnabled = isRunning;

            if (isRunning)
            {
                StatusText.Text = "Service Running";
                StatusIndicator.Fill = new SolidColorBrush(Color.FromRgb(76, 175, 80));
                HealthText.Text = "Healthy";
                StatusBarText.Text = "Service is running";
            }
            else
            {
                StatusText.Text = "Service Stopped";
                StatusIndicator.Fill = new SolidColorBrush(Color.FromRgb(255, 82, 82));
                HealthText.Text = "Not Healthy";
                StatusBarText.Text = "Service stopped";
            }
        });
    }

    private void ServiceManager_ErrorOccurred(object? sender, string e)
    {
        Dispatcher.Invoke(() =>
        {
            DebugTextBox.AppendText($"[ERROR] {e}\n");
            DebugTextBox.ScrollToEnd();
        });
    }

    private void UpdateApiUrl()
    {
        var host = HostTextBox.Text;
        var port = PortTextBox.Text;
        ApiUrlText.Text = $"API: http://{host}:{port}";
        _apiClient.SetBaseUrl(host, int.TryParse(port, out var p) ? p : 17878);
    }

    private void ShowError(string title, string message)
    {
        MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error);
    }
}
