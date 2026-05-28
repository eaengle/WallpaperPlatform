using System.Windows;

namespace WallpaperPlatform;

public partial class App : System.Windows.Application
{
    private SystemTrayHelper? _tray;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        ShutdownMode = ShutdownMode.OnExplicitShutdown;

        var window = new MainWindow();
        window.Show();

        _tray = new SystemTrayHelper(window);
        _tray.ExitRequested += Shutdown;
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _tray?.Dispose();
        base.OnExit(e);
    }
}
