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
            Icon             = LoadAppIcon(),
            Text             = "Wallpaper Platform",
            Visible          = true,
            ContextMenuStrip = BuildMenu(),
        };
    }

    private static Icon LoadAppIcon()
    {
        var stream = typeof(SystemTrayHelper).Assembly
            .GetManifestResourceStream("WallpaperPlatform.app.ico");
        return stream is not null ? new Icon(stream) : SystemIcons.Application;
    }

    private ContextMenuStrip BuildMenu()
    {
        var menu = new ContextMenuStrip();

        var events = new ToolStripMenuItem("Trigger Event");
        events.DropDownItems.Add("Shooting Star",  null, (_, _) => _window.FireEvent("shooting_star"));
        events.DropDownItems.Add("Blizzard Surge", null, (_, _) => _window.FireEvent("blizzard_surge"));
        events.DropDownItems.Add("Cabin Flicker",  null, (_, _) => _window.FireEvent("cabin_flicker"));
        menu.Items.Add(events);

        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Exit", null, (_, _) => ExitRequested?.Invoke());
        return menu;
    }

    public void Dispose()
    {
        _icon.Visible = false;
        _icon.Dispose();
    }
}
