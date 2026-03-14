using System.Diagnostics;
using Clipboard = System.Windows.Clipboard;

namespace SyncTray;

public sealed class MenuBuilder
{
    private readonly StatusMonitor _statusMonitor;
    private readonly Action _quitAction;

    public MenuBuilder(StatusMonitor statusMonitor, Action quitAction)
    {
        _statusMonitor = statusMonitor;
        _quitAction = quitAction;
    }

    public void Build(ContextMenuStrip menu)
    {
        menu.Items.Clear();

        // Header
        var header = new ToolStripMenuItem($"{Constants.AppName} ({Constants.Version})")
        {
            Enabled = false,
            Font = new System.Drawing.Font(menu.Font, System.Drawing.FontStyle.Bold),
        };
        menu.Items.Add(header);

        menu.Items.Add(new ToolStripSeparator());

        // Devices submenu
        var devicesMenu = new ToolStripMenuItem("Devices");
        foreach (var device in _statusMonitor.Devices.Values)
        {
            string prefix;
            if (device.Error)
                prefix = "\u26A0"; // ⚠
            else if (device.Connected)
                prefix = "\u25CF"; // ●
            else
                prefix = "\u25CB"; // ○

            var item = new ToolStripMenuItem($"{prefix} {device.Name}") { Enabled = false };
            devicesMenu.DropDownItems.Add(item);
        }
        if (devicesMenu.DropDownItems.Count == 0)
        {
            devicesMenu.DropDownItems.Add(new ToolStripMenuItem("No devices") { Enabled = false });
        }
        menu.Items.Add(devicesMenu);

        // Folders submenu
        var foldersMenu = new ToolStripMenuItem("Folders");
        foreach (var folder in _statusMonitor.Folders.Values)
        {
            var prefix = folder.HasError ? "!" : "\u2713"; // ✓
            var item = new ToolStripMenuItem($"{prefix} {folder.Label}") { Enabled = false };
            foldersMenu.DropDownItems.Add(item);
        }
        if (foldersMenu.DropDownItems.Count == 0)
        {
            foldersMenu.DropDownItems.Add(new ToolStripMenuItem("No folders") { Enabled = false });
        }
        menu.Items.Add(foldersMenu);

        // Open web portal
        var openWeb = new ToolStripMenuItem("Open web portal");
        openWeb.Click += (_, _) =>
        {
            Process.Start(
                new ProcessStartInfo { FileName = _statusMonitor.BaseUrl, UseShellExecute = true }
            );
        };
        menu.Items.Add(openWeb);

        // Copy device ID
        var copyId = new ToolStripMenuItem("Copy device ID");
        copyId.Click += (_, _) =>
        {
            var id = _statusMonitor.MyDeviceId;
            if (!string.IsNullOrEmpty(id))
                Clipboard.SetText(id);
        };
        menu.Items.Add(copyId);

        // Quit
        var quit = new ToolStripMenuItem("Quit");
        quit.Click += (_, _) => _quitAction();
        menu.Items.Add(quit);
    }
}
