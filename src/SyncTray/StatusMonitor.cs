using System.Text.Json;
using LibSyncthing;
using LibSyncthing.Endpoints;
using LibSyncthing.Models;

namespace SyncTray;

public sealed class DeviceState
{
    public required string DeviceId { get; init; }
    public required string Name { get; init; }
    public bool Connected { get; set; }
    public bool Error { get; set; }
}

public sealed class FolderState
{
    public required string FolderId { get; init; }
    public required string Label { get; init; }
    public string State { get; set; } = "";
    public bool HasError { get; set; }
}

public sealed class StatusMonitor : IDisposable
{
    private readonly SyncthingClient _client;
    private CancellationTokenSource _cts = new();
    private long _lastEventId;
    private string _myDeviceId = "";

    public string MyDeviceId => _myDeviceId;
    public Dictionary<string, DeviceState> Devices { get; } = new();
    public Dictionary<string, FolderState> Folders { get; } = new();
    public string BaseUrl { get; }

    public event Action? StateChanged;

    public StatusMonitor(SyncthingClient client, string baseUrl)
    {
        _client = client;
        BaseUrl = baseUrl;
    }

    public async Task InitializeAsync()
    {
        // Get own device ID
        var status = await _client.GetSystemStatusAsync();
        _myDeviceId = status.MyId;

        // Load config to get devices and folders
        var config = await _client.GetConfigAsync();

        foreach (var device in config.Devices)
        {
            // Skip our own device
            if (device.DeviceId == _myDeviceId)
                continue;

            Devices[device.DeviceId] = new DeviceState
            {
                DeviceId = device.DeviceId,
                Name = string.IsNullOrEmpty(device.Name) ? device.DeviceId[..8] : device.Name
            };
        }

        foreach (var folder in config.Folders)
        {
            Folders[folder.Id] = new FolderState
            {
                FolderId = folder.Id,
                Label = string.IsNullOrEmpty(folder.Label) ? folder.Id : folder.Label
            };
        }

        // Initial connection status
        await PollConnectionsAsync();
        await PollFolderStatusesAsync();
    }

    public void Start()
    {
        _cts = new CancellationTokenSource();
        _ = EventLoopAsync(_cts.Token);
        _ = PollingLoopAsync(_cts.Token);
    }

    public void Stop()
    {
        _cts.Cancel();
    }

    private async Task EventLoopAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                var events = await _client.GetEventsAsync(_lastEventId, ct);
                foreach (var evt in events)
                {
                    _lastEventId = evt.Id;
                    ProcessEvent(evt);
                }
            }
            catch (OperationCanceledException) { break; }
            catch (Exception)
            {
                // On error, wait before retrying
                try { await Task.Delay(5000, ct); } catch { break; }
            }
        }
    }

    private void ProcessEvent(Event evt)
    {
        switch (evt.Type)
        {
            case "DeviceConnected":
                if (TryGetDataString(evt, "id", out var connectedId) && Devices.ContainsKey(connectedId))
                {
                    Devices[connectedId].Connected = true;
                    Devices[connectedId].Error = false;
                    StateChanged?.Invoke();
                }
                break;

            case "DeviceDisconnected":
                if (TryGetDataString(evt, "id", out var disconnectedId) && Devices.ContainsKey(disconnectedId))
                {
                    Devices[disconnectedId].Connected = false;
                    StateChanged?.Invoke();
                }
                break;

            case "StateChanged":
                if (TryGetDataString(evt, "folder", out var folderId) && Folders.ContainsKey(folderId))
                {
                    TryGetDataString(evt, "to", out var toState);
                    Folders[folderId].State = toState ?? "";
                    Folders[folderId].HasError = toState == "error";
                    StateChanged?.Invoke();
                }
                break;

            case "FolderErrors":
                if (TryGetDataString(evt, "folder", out var errorFolderId) && Folders.ContainsKey(errorFolderId))
                {
                    Folders[errorFolderId].HasError = true;
                    StateChanged?.Invoke();
                }
                break;

            case "ConfigSaved":
                // Re-read config
                _ = Task.Run(async () =>
                {
                    try { await RefreshConfigAsync(); } catch { }
                });
                break;
        }
    }

    private static bool TryGetDataString(Event evt, string key, out string value)
    {
        value = "";
        if (evt.Data == null || !evt.Data.TryGetValue(key, out var obj))
            return false;

        if (obj is JsonElement je)
        {
            value = je.GetString() ?? "";
            return !string.IsNullOrEmpty(value);
        }

        value = obj?.ToString() ?? "";
        return !string.IsNullOrEmpty(value);
    }

    private async Task RefreshConfigAsync()
    {
        var config = await _client.GetConfigAsync();
        foreach (var device in config.Devices)
        {
            if (device.DeviceId == _myDeviceId) continue;
            if (!Devices.ContainsKey(device.DeviceId))
            {
                Devices[device.DeviceId] = new DeviceState
                {
                    DeviceId = device.DeviceId,
                    Name = string.IsNullOrEmpty(device.Name) ? device.DeviceId[..8] : device.Name
                };
            }
        }

        foreach (var folder in config.Folders)
        {
            if (!Folders.ContainsKey(folder.Id))
            {
                Folders[folder.Id] = new FolderState
                {
                    FolderId = folder.Id,
                    Label = string.IsNullOrEmpty(folder.Label) ? folder.Id : folder.Label
                };
            }
        }

        StateChanged?.Invoke();
    }

    private async Task PollingLoopAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(Constants.PollIntervalSeconds), ct);
                await PollConnectionsAsync();
                await PollFolderStatusesAsync();
                StateChanged?.Invoke();
            }
            catch (OperationCanceledException) { break; }
            catch { /* ignore transient errors, will retry next interval */ }
        }
    }

    private async Task PollConnectionsAsync()
    {
        var connections = await _client.GetConnectionsAsync();
        foreach (var (deviceId, info) in connections.Connections)
        {
            if (Devices.TryGetValue(deviceId, out var state))
                state.Connected = info.Connected;
        }
    }

    private async Task PollFolderStatusesAsync()
    {
        foreach (var (folderId, state) in Folders)
        {
            try
            {
                var status = await _client.GetFolderStatusAsync(folderId);
                state.State = status.State;
                state.HasError = status.Errors > 0 || status.State == "error";
            }
            catch { /* skip this folder, retry next poll */ }
        }
    }

    public void Dispose()
    {
        _cts.Cancel();
        _cts.Dispose();
    }
}
