using System.Drawing;
using System.Windows.Forms;

namespace WallpaperPlatform;

internal sealed class SystemTrayHelper : IDisposable
{
    private readonly NotifyIcon _icon;
    private readonly WallpaperPlatform.MainWindow _window;

    public event Action? ExitRequested;

    public SystemTrayHelper(WallpaperPlatform.MainWindow window)
    {
        _window = window;
        _icon = new NotifyIcon
        {
            Icon = SystemIcons.Application,
            Text = "Wallpaper Platform",
            Visible = true,
            ContextMenuStrip = BuildMenu()
        };
    }

    private ContextMenuStrip BuildMenu()
    {
        var menu = new ContextMenuStrip();
        menu.Items.Add("Exit", null, (_, _) => ExitRequested?.Invoke());
        return menu;
    }

    public void Dispose()
    {
        _icon.Visible = false;
        _icon.Dispose();
    }
}
