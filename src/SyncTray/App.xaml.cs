using System.Windows;
using LibSyncthing;
using LibSyncthing.Endpoints;
using Application = System.Windows.Application;
using MessageBox = System.Windows.MessageBox;

namespace SyncTray;

public partial class App : Application
{
    private Mutex? _mutex;
    private SyncthingClient? _client;
    private SyncthingProcessManager? _processManager;
    private StatusMonitor? _statusMonitor;
    private TrayIconManager? _trayIconManager;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Single-instance guard
        _mutex = new Mutex(true, "SyncTray_SingleInstance", out var createdNew);
        if (!createdNew)
        {
            MessageBox.Show(
                "SyncTray is already running.",
                Constants.AppName,
                MessageBoxButton.OK,
                MessageBoxImage.Information
            );
            Shutdown();
            return;
        }

        // Handle session ending (system shutdown / logoff)
        SessionEnding += OnSessionEnding;

        // Load settings
        var settingsManager = new SettingsManager();
        settingsManager.Load();

        // Read configuration
        var configReader = new ConfigReader(settingsManager);
        if (!configReader.ReadConfig())
        {
            Shutdown();
            return;
        }

        var baseUrl = configReader.GetBaseUrl();
        var apiKey = settingsManager.Settings.ApiKey!;
        var port = settingsManager.Settings.SyncthingPort ?? Constants.DefaultPort;

        // Create API client
        _client = new SyncthingClient(baseUrl, apiKey);

        // Ensure Syncthing is running
        _processManager = new SyncthingProcessManager(_client, port);
        if (!await _processManager.EnsureRunningAsync())
        {
            Shutdown();
            return;
        }

        // Wait briefly for Syncthing to be ready if we just started it
        await WaitForSyncthingReady(_client);

        // Initialize status monitor
        _statusMonitor = new StatusMonitor(_client, baseUrl);
        try
        {
            await _statusMonitor.InitializeAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Failed to connect to Syncthing API:\n{ex.Message}",
                Constants.AppName,
                MessageBoxButton.OK,
                MessageBoxImage.Error
            );
            Shutdown();
            return;
        }

        // Set up tray icon
        var menuBuilder = new MenuBuilder(_statusMonitor, QuitApplication);
        _trayIconManager = new TrayIconManager(menuBuilder);

        // Subscribe to state changes to refresh menu
        _statusMonitor.StateChanged += () =>
        {
            Dispatcher.Invoke(() => _trayIconManager.RefreshMenu());
        };

        // Handle unexpected daemon exit
        _processManager.ProcessExitedUnexpectedly += () =>
        {
            var result = MessageBox.Show(
                "The Syncthing process has exited unexpectedly.\n\nRestart it?",
                Constants.AppName,
                MessageBoxButton.YesNo,
                MessageBoxImage.Error
            );

            if (result == MessageBoxResult.Yes)
            {
                _ = _processManager.EnsureRunningAsync();
            }
            else
            {
                QuitApplication();
            }
        };

        // Start status monitoring
        _statusMonitor.Start();
    }

    private static async Task WaitForSyncthingReady(SyncthingClient client)
    {
        for (var i = 0; i < 30; i++)
        {
            try
            {
                await client.PingAsync();
                return;
            }
            catch
            {
                await Task.Delay(1000);
            }
        }
    }

    private async void QuitApplication()
    {
        _statusMonitor?.Stop();

        if (_processManager != null)
            await _processManager.ShutdownAsync();

        _trayIconManager?.Dispose();
        _processManager?.Dispose();
        _statusMonitor?.Dispose();
        _client?.Dispose();
        _mutex?.Dispose();

        Shutdown();
    }

    private async void OnSessionEnding(object sender, SessionEndingCancelEventArgs e)
    {
        _statusMonitor?.Stop();

        if (_processManager != null)
            await _processManager.ShutdownAsync();

        _trayIconManager?.Dispose();
        _processManager?.Dispose();
        _statusMonitor?.Dispose();
        _client?.Dispose();
    }
}
