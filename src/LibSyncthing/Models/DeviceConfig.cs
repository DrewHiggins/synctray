using System.Text.Json.Serialization;

namespace LibSyncthing.Models;

public sealed class DeviceConfig
{
    [JsonPropertyName("deviceID")]
    public string DeviceId { get; set; } = "";

    [JsonPropertyName("name")]
    public string Name { get; set; } = "";
}
