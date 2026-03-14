using System.Text.Json.Serialization;

namespace LibSyncthing.Models;

public sealed class FolderConfig
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = "";

    [JsonPropertyName("label")]
    public string Label { get; set; } = "";

    [JsonPropertyName("path")]
    public string Path { get; set; } = "";
}
