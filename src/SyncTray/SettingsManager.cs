using System.IO;
using System.Text.Json;

namespace SyncTray;

public sealed class AppSettings
{
    public string? ConfigFilePath { get; set; }
    public string? SyncthingHost { get; set; }
    public int? SyncthingPort { get; set; }
    public bool UseTls { get; set; }
    public string? ApiKey { get; set; }
}

public sealed class SettingsManager
{
    private static readonly string SettingsDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "SyncTray"
    );

    private static readonly string SettingsFile = Path.Combine(SettingsDir, "settings.json");

    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public AppSettings Settings { get; private set; } = new();

    public void Load()
    {
        if (!File.Exists(SettingsFile))
            return;

        var json = File.ReadAllText(SettingsFile);
        Settings = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions) ?? new AppSettings();
    }

    public void Save()
    {
        Directory.CreateDirectory(SettingsDir);
        var json = JsonSerializer.Serialize(Settings, JsonOptions);
        File.WriteAllText(SettingsFile, json);
    }
}
