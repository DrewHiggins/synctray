using LibSyncthing.Models;

namespace LibSyncthing.Endpoints;

public static class DbEndpoints
{
    public static Task<FolderStatus> GetFolderStatusAsync(this SyncthingClient client, string folderId, CancellationToken ct = default)
        => client.GetAsync<FolderStatus>($"/rest/db/status?folder={Uri.EscapeDataString(folderId)}", ct);
}
