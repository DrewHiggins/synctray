using System.Text.Json.Serialization;

namespace LibSyncthing.Models;

public sealed class ConnectionInfo
{
    [JsonPropertyName("connected")]
    public bool Connected { get; set; }

    [JsonPropertyName("address")]
    public string Address { get; set; } = "";
}

public sealed class ConnectionsResponse
{
    [JsonPropertyName("connections")]
    public Dictionary<string, ConnectionInfo> Connections { get; set; } = new();
}
