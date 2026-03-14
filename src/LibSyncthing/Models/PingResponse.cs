using System.Text.Json.Serialization;

namespace LibSyncthing.Models;

public sealed class PingResponse
{
    [JsonPropertyName("ping")]
    public string Ping { get; set; } = "";
}
