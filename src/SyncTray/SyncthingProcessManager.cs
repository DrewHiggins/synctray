using System.Diagnostics;
using System.IO;
using System.Net.NetworkInformation;
using System.Windows;
using LibSyncthing;
using LibSyncthing.Endpoints;
using Application = System.Windows.Application;
using MessageBox = System.Windows.MessageBox;

namespace SyncTray;

public sealed class SyncthingProcessManager : IDisposable
{
    private Process? _process;
    private readonly SyncthingClient _client;
    private readonly int _port;
    private bool _disposed;
    private bool _intentionalShutdown;

    public event Action? ProcessExitedUnexpectedly;

    public SyncthingProcessManager(SyncthingClient client, int port)
    {
        _client = client;
        _port = port;
    }

    public async Task<bool> EnsureRunningAsync()
    {
        if (IsPortInUse(_port))
        {
            // Something is listening — verify it's Syncthing
            try
            {
                var ping = await _client.PingAsync();
                return ping.Ping == "pong";
            }
            catch
            {
                MessageBox.Show(
                    $"Port {_port} is in use but does not appear to be a Syncthing instance.\n"
                        + "Please close the conflicting application and try again.",
                    Constants.AppName,
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
                return false;
            }
        }

        return StartProcess();
    }

    private bool StartProcess()
    {
        if (!File.Exists(Constants.SyncthingExePath))
        {
            MessageBox.Show(
                $"Syncthing executable not found at:\n{Constants.SyncthingExePath}",
                Constants.AppName,
                MessageBoxButton.OK,
                MessageBoxImage.Error
            );
            return false;
        }

        _process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = Constants.SyncthingExePath,
                Arguments = "--no-browser",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
            },
            EnableRaisingEvents = true,
        };

        _process.OutputDataReceived += OnOutputData;
        _process.ErrorDataReceived += OnErrorData;
        _process.Exited += OnProcessExited;

        try
        {
            _process.Start();
            _process.BeginOutputReadLine();
            _process.BeginErrorReadLine();
            return true;
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Failed to start Syncthing:\n{ex.Message}",
                Constants.AppName,
                MessageBoxButton.OK,
                MessageBoxImage.Error
            );
            return false;
        }
    }

    private void OnOutputData(object sender, DataReceivedEventArgs e)
    {
        if (e.Data == null)
            return;
        CheckForFatalError(e.Data);
    }

    private void OnErrorData(object sender, DataReceivedEventArgs e)
    {
        if (e.Data == null)
            return;
        CheckForFatalError(e.Data);
    }

    private void CheckForFatalError(string line)
    {
        if (line.Contains("FATAL", StringComparison.OrdinalIgnoreCase))
        {
            Application.Current?.Dispatcher.Invoke(() =>
            {
                MessageBox.Show(
                    $"Syncthing reported a fatal error:\n{line}",
                    Constants.AppName,
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            });
        }
    }

    private void OnProcessExited(object? sender, EventArgs e)
    {
        if (_intentionalShutdown || _disposed)
            return;

        Application.Current?.Dispatcher.Invoke(() =>
        {
            ProcessExitedUnexpectedly?.Invoke();
        });
    }

    public async Task ShutdownAsync()
    {
        _intentionalShutdown = true;
        if (_process == null || _process.HasExited)
            return;

        try
        {
            await _client.ShutdownAsync();
            // Give it a few seconds to exit gracefully
            if (!_process.WaitForExit(5000))
                _process.Kill();
        }
        catch
        {
            try
            {
                _process.Kill();
            }
            catch
            { /* already exited */
            }
        }
    }

    private static bool IsPortInUse(int port)
    {
        var listeners = IPGlobalProperties.GetIPGlobalProperties().GetActiveTcpListeners();
        return listeners.Any(ep => ep.Port == port);
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;

        if (_process != null && !_process.HasExited)
        {
            try
            {
                _process.Kill();
            }
            catch { }
        }
        _process?.Dispose();
    }
}
