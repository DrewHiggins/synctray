using System.Text.Json.Serialization;

namespace LibSyncthing.Models;

public sealed class SystemConfig
{
    [JsonPropertyName("devices")]
    public List<DeviceConfig> Devices { get; set; } = [];

    [JsonPropertyName("folders")]
    public List<FolderConfig> Folders { get; set; } = [];
}
