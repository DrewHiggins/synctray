using LibSyncthing.Models;

namespace LibSyncthing.Endpoints;

public static class SystemEndpoints
{
    public static Task<PingResponse> PingAsync(
        this SyncthingClient client,
        CancellationToken ct = default
    ) => client.GetAsync<PingResponse>("/rest/system/ping", ct);

    public static Task<SystemStatus> GetSystemStatusAsync(
        this SyncthingClient client,
        CancellationToken ct = default
    ) => client.GetAsync<SystemStatus>("/rest/system/status", ct);

    public static Task<SystemConfig> GetConfigAsync(
        this SyncthingClient client,
        CancellationToken ct = default
    ) => client.GetAsync<SystemConfig>("/rest/system/config", ct);

    public static Task ShutdownAsync(this SyncthingClient client, CancellationToken ct = default) =>
        client.PostAsync("/rest/system/shutdown", ct);
}
