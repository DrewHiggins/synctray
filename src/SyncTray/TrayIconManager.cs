namespace SyncTray;

public sealed class TrayIconManager : IDisposable
{
    private readonly NotifyIcon _notifyIcon;
    private readonly ContextMenuStrip _contextMenu;
    private readonly MenuBuilder _menuBuilder;
    private bool _disposed;

    public TrayIconManager(MenuBuilder menuBuilder)
    {
        _menuBuilder = menuBuilder;
        _contextMenu = new ContextMenuStrip();

        _notifyIcon = new NotifyIcon
        {
            Icon = LoadIcon(),
            Text = Constants.AppName,
            Visible = true,
            ContextMenuStrip = _contextMenu,
        };

        // Rebuild menu every time it opens to reflect current state
        _contextMenu.Opening += (_, _) => _menuBuilder.Build(_contextMenu);
    }

    public void RefreshMenu()
    {
        _menuBuilder.Build(_contextMenu);
    }

    private static Icon LoadIcon()
    {
        var uri = new Uri("pack://application:,,,/Resources/logo.ico", UriKind.Absolute);
        var stream = System.Windows.Application.GetResourceStream(uri)?.Stream;
        return stream != null ? new Icon(stream) : SystemIcons.Application;
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;

        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
        _contextMenu.Dispose();
    }
}
