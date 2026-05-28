using System.Drawing;
using System.IO;
using System.Text.Json;
using System.Windows.Forms;

namespace WallpaperPlatform;

internal sealed class SystemTrayHelper : IDisposable
{
    private readonly NotifyIcon _icon;
    private readonly WallpaperPlatform.MainWindow _window;
    private ToolStripMenuItem _scenesMenu = null!;
    private ToolStripMenuItem _eventsMenu = null!;

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
        menu.Opening += OnMenuOpening;

        _scenesMenu = new ToolStripMenuItem("Select Scene");
        foreach (var (folder, displayName) in EnumerateWallpapers())
        {
            var captured = folder;
            var item = new ToolStripMenuItem(displayName) { Tag = captured };
            item.Click += (_, _) => _window.LoadWallpaper(captured);
            _scenesMenu.DropDownItems.Add(item);
        }
        menu.Items.Add(_scenesMenu);

        menu.Items.Add(new ToolStripSeparator());

        _eventsMenu = new ToolStripMenuItem("Trigger Event");
        menu.Items.Add(_eventsMenu);

        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Exit", null, (_, _) => ExitRequested?.Invoke());
        return menu;
    }

    private void OnMenuOpening(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        foreach (ToolStripMenuItem item in _scenesMenu.DropDownItems)
            item.Checked = item.Tag is string f && f == _window.CurrentWallpaper;

        _eventsMenu.DropDownItems.Clear();
        foreach (var ev in _window.CurrentWallpaperEvents)
        {
            var name = ev.Name;
            _eventsMenu.DropDownItems.Add(ev.Label, null, (_, _) => _window.FireEvent(name));
        }
        _eventsMenu.Enabled = _eventsMenu.DropDownItems.Count > 0;
    }

    private static IEnumerable<(string folder, string displayName)> EnumerateWallpapers()
    {
        var wallpapersDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "wallpapers");
        if (!Directory.Exists(wallpapersDir)) yield break;

        foreach (var dir in Directory.GetDirectories(wallpapersDir))
        {
            var manifestPath = Path.Combine(dir, "manifest.json");
            if (!File.Exists(manifestPath)) continue;

            var folder = Path.GetFileName(dir);
            var displayName = folder;
            try
            {
                using var doc = JsonDocument.Parse(File.ReadAllText(manifestPath));
                if (doc.RootElement.TryGetProperty("name", out var prop))
                    displayName = prop.GetString() ?? folder;
            }
            catch { }

            yield return (folder, displayName);
        }
    }

    public void Dispose()
    {
        _icon.Visible = false;
        _icon.Dispose();
    }
}
