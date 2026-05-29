using System.Drawing;
using System.IO;
using System.Text.Json;
using System.Windows.Forms;
using Microsoft.Win32;

namespace WallpaperPlatform;

internal sealed class SystemTrayHelper : IDisposable
{
    private readonly NotifyIcon _icon;
    private MainWindow? _window;
    private ToolStripMenuItem? _scenesMenu;
    private ToolStripMenuItem? _eventsMenu;

    public event Action? StartRequested;
    public event Action? StopRequested;
    public event Action? ExitRequested;

    public SystemTrayHelper()
    {
        _icon = new NotifyIcon
        {
            Icon    = LoadAppIcon(),
            Text    = "Wallpaper Platform",
            Visible = true,
        };
        SetWaitMode();
    }

    public void SetWaitMode()
    {
        _window = null;
        var old = _icon.ContextMenuStrip;
        _icon.ContextMenuStrip = BuildWaitMenu();
        old?.Dispose();
    }

    public void SetRunMode(MainWindow window)
    {
        _window = window;
        var old = _icon.ContextMenuStrip;
        _icon.ContextMenuStrip = BuildRunMenu();
        old?.Dispose();
    }

    private ContextMenuStrip BuildWaitMenu()
    {
        var menu = new ContextMenuStrip();
        menu.Items.Add("Start Wallpaper", null, (_, _) => StartRequested?.Invoke());
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(BuildStartupItem());
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Exit", null, (_, _) => ExitRequested?.Invoke());
        return menu;
    }

    private ContextMenuStrip BuildRunMenu()
    {
        var menu = new ContextMenuStrip();
        menu.Opening += OnMenuOpening;

        _scenesMenu = new ToolStripMenuItem("Select Scene");
        foreach (var (folder, displayName) in EnumerateWallpapers())
        {
            var captured = folder;
            var item = new ToolStripMenuItem(displayName) { Tag = captured };
            item.Click += (_, _) => _window?.LoadWallpaper(captured);
            _scenesMenu.DropDownItems.Add(item);
        }
        menu.Items.Add(_scenesMenu);

        menu.Items.Add(new ToolStripSeparator());

        _eventsMenu = new ToolStripMenuItem("Trigger Event");
        menu.Items.Add(_eventsMenu);

        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Stop Wallpaper", null, (_, _) => StopRequested?.Invoke());
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(BuildStartupItem());
        return menu;
    }

    private static ToolStripMenuItem BuildStartupItem()
    {
        var item = new ToolStripMenuItem("Start with Windows")
        {
            Checked      = IsStartupEnabled(),
            CheckOnClick = true,
        };
        item.Click += (_, _) => SetStartup(item.Checked);
        return item;
    }

    private void OnMenuOpening(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        if (_scenesMenu != null)
            foreach (ToolStripMenuItem item in _scenesMenu.DropDownItems)
                item.Checked = item.Tag is string f && f == _window?.CurrentWallpaper;

        if (_eventsMenu != null)
        {
            _eventsMenu.DropDownItems.Clear();
            if (_window != null)
                foreach (var ev in _window.CurrentWallpaperEvents)
                {
                    var name = ev.Name;
                    _eventsMenu.DropDownItems.Add(ev.Label, null, (_, _) => _window.FireEvent(name));
                }
            _eventsMenu.Enabled = _eventsMenu.DropDownItems.Count > 0;
        }
    }

    // ── Startup registry ────────────────────────────────────────────────────

    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string AppName    = "WallpaperPlatform";

    private static bool IsStartupEnabled()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: false);
        return key?.GetValue(AppName) is string;
    }

    private static void SetStartup(bool enable)
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: true)!;
        if (enable)
        {
            var exePath = Environment.ProcessPath;
            if (!string.IsNullOrEmpty(exePath))
                key.SetValue(AppName, exePath);
        }
        else
        {
            key.DeleteValue(AppName, throwOnMissingValue: false);
        }
    }

    // ── Helpers ─────────────────────────────────────────────────────────────

    private static Icon LoadAppIcon()
    {
        var stream = typeof(SystemTrayHelper).Assembly
            .GetManifestResourceStream("WallpaperPlatform.app.ico");
        return stream is not null ? new Icon(stream) : SystemIcons.Application;
    }

    private static IEnumerable<(string folder, string displayName)> EnumerateWallpapers()
    {
        var wallpapersDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "wallpapers");
        if (!Directory.Exists(wallpapersDir)) yield break;

        foreach (var dir in Directory.GetDirectories(wallpapersDir))
        {
            var manifestPath = Path.Combine(dir, "manifest.json");
            if (!File.Exists(manifestPath)) continue;

            var folder      = Path.GetFileName(dir);
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
