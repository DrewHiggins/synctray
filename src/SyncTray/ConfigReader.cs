using System.IO;
using System.Xml.Linq;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;

namespace SyncTray;

public sealed class ConfigReader
{
    private static readonly string DefaultConfigPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Syncthing",
        "config.xml"
    );

    private readonly SettingsManager _settingsManager;

    public ConfigReader(SettingsManager settingsManager)
    {
        _settingsManager = settingsManager;
    }

    /// <summary>
    /// Reads the Syncthing configuration.xml, populates settings, and returns true on success.
    /// Returns false if the user cancels browsing for the config file.
    /// </summary>
    public bool ReadConfig()
    {
        var configPath = ResolveConfigPath();
        if (configPath == null)
            return false;

        var doc = XDocument.Load(configPath);
        var gui = doc.Root?.Element("gui");
        if (gui == null)
            throw new InvalidOperationException(
                "Invalid configuration.xml: missing <gui> element."
            );

        var apiKey = gui.Element("apikey")?.Value;
        if (string.IsNullOrWhiteSpace(apiKey))
            throw new InvalidOperationException("Invalid configuration.xml: missing API key.");

        var address =
            gui.Element("address")?.Value ?? $"{Constants.DefaultHost}:{Constants.DefaultPort}";
        var tlsAttr = gui.Attribute("tls")?.Value;
        var useTls = string.Equals(tlsAttr, "true", StringComparison.OrdinalIgnoreCase);

        ParseAddress(address, out var host, out var port);

        _settingsManager.Settings.ConfigFilePath = configPath;
        _settingsManager.Settings.ApiKey = apiKey;
        _settingsManager.Settings.SyncthingHost = host;
        _settingsManager.Settings.SyncthingPort = port;
        _settingsManager.Settings.UseTls = useTls;
        _settingsManager.Save();

        return true;
    }

    public string GetBaseUrl()
    {
        var s = _settingsManager.Settings;
        var scheme = s.UseTls ? "https" : "http";
        return $"{scheme}://{s.SyncthingHost}:{s.SyncthingPort}";
    }

    private string? ResolveConfigPath()
    {
        // Try saved path first
        var saved = _settingsManager.Settings.ConfigFilePath;
        if (!string.IsNullOrEmpty(saved) && File.Exists(saved))
            return saved;

        // Try default path
        if (File.Exists(DefaultConfigPath))
            return DefaultConfigPath;

        // Prompt user to browse
        var dialog = new OpenFileDialog
        {
            Title = "Locate Syncthing configuration.xml",
            Filter = "XML files (*.xml)|*.xml|All files (*.*)|*.*",
            FileName = "configuration.xml",
        };

        return dialog.ShowDialog() == true ? dialog.FileName : null;
    }

    private static void ParseAddress(string address, out string host, out int port)
    {
        // Handle IPv6 bracket notation e.g. [::]:8384
        if (address.StartsWith('['))
        {
            var closeBracket = address.IndexOf(']');
            host = address[1..closeBracket];
            var portPart = address[(closeBracket + 1)..];
            port = portPart.StartsWith(':') ? int.Parse(portPart[1..]) : Constants.DefaultPort;
        }
        else
        {
            var parts = address.Split(':');
            if (parts.Length == 2)
            {
                host = parts[0];
                port = int.Parse(parts[1]);
            }
            else
            {
                host = address;
                port = Constants.DefaultPort;
            }
        }

        // Syncthing uses 0.0.0.0 or :: to mean "all interfaces" — connect via localhost
        if (host is "0.0.0.0" or "::" or "")
            host = Constants.DefaultHost;
    }
}
