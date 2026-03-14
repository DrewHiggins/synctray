using LibSyncthing.Models;

namespace LibSyncthing.Endpoints;

public static class ConnectionEndpoints
{
    public static Task<ConnectionsResponse> GetConnectionsAsync(
        this SyncthingClient client,
        CancellationToken ct = default
    ) => client.GetAsync<ConnectionsResponse>("/rest/system/connections", ct);
}
