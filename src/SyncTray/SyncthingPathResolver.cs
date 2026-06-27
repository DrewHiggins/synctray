using Microsoft.Win32;

namespace SyncTray;

/// <summary>
/// Resolves the location of the Syncthing executable.
/// The installer records the chosen/installed path in the registry; if that is
/// absent we fall back to the default install location.
/// </summary>
public static class SyncthingPathResolver
{
    private const string RegistryKeyPath = @"SOFTWARE\SyncTray";
    private const string RegistryValueName = "SyncthingPath";

    public static string Resolve()
    {
        var fromRegistry = ReadFromRegistry();
        return string.IsNullOrWhiteSpace(fromRegistry)
            ? Constants.SyncthingExePath
            : fromRegistry!;
    }

    private static string? ReadFromRegistry()
    {
        try
        {
            using var baseKey = RegistryKey.OpenBaseKey(
                RegistryHive.LocalMachine,
                RegistryView.Registry64
            );
            using var key = baseKey.OpenSubKey(RegistryKeyPath);
            return key?.GetValue(RegistryValueName) as string;
        }
        catch
        {
            return null;
        }
    }
}
