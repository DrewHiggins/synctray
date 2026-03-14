using System.Text.Json.Serialization;

namespace LibSyncthing.Models;

public sealed class FolderStatus
{
    [JsonPropertyName("state")]
    public string State { get; set; } = "";

    [JsonPropertyName("errors")]
    public int Errors { get; set; }

    [JsonPropertyName("globalFiles")]
    public long GlobalFiles { get; set; }

    [JsonPropertyName("inSyncFiles")]
    public long InSyncFiles { get; set; }
}
