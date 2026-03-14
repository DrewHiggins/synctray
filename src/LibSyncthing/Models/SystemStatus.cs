using System.Text.Json.Serialization;

namespace LibSyncthing.Models;

public sealed class SystemStatus
{
    [JsonPropertyName("myID")]
    public string MyId { get; set; } = "";

    [JsonPropertyName("uptime")]
    public long Uptime { get; set; }
}
