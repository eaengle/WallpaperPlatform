# WallpaperPlatform

An open-source Windows desktop wallpaper host that renders animated, event-driven wallpapers using a WebView2 (Chromium) engine embedded behind your desktop icons.

Wallpapers are self-contained HTML/JS/WebGL packages — the same web skills you already have. The platform is free and open-source; premium wallpaper packs are the monetization layer.

---

## Features

- Renders wallpapers **behind desktop icons** using the Windows `WorkerW` layer
- Full **4K / multi-DPI** support
- Wallpapers are plain **HTML + JavaScript** — use Canvas, WebGL, Three.js, anything
- **System tray** icon for control (no taskbar clutter)
- Wallpaper packages defined by a simple `manifest.json`
- **Event bridge** — C# posts `postMessage` events to wallpapers for real-time scene control

---

## Requirements

- Windows 10 / 11
- [.NET 9 SDK](https://dotnet.microsoft.com/download)
- WebView2 Runtime — **pre-installed on Windows 11**; for Windows 10 it ships with Microsoft Edge

---

## Getting Started

```bash
git clone https://github.com/eaengle/WallpaperPlatform.git
cd WallpaperPlatform
dotnet run --project WallpaperPlatform
```

Exit via the system tray icon (right-click → Exit).

---

## Wallpaper Package Format

Each wallpaper lives in its own folder under `wallpapers/`:

```
wallpapers/
└── my-wallpaper/
    ├── manifest.json
    └── index.html
```

### manifest.json

```json
{
  "name": "My Wallpaper",
  "version": "1.0.0",
  "description": "A short description",
  "tier": "free"
}
```

| Field | Values |
|---|---|
| `tier` | `"free"` or `"premium"` |

### index.html

A standard HTML page. It has access to the full screen dimensions via `window.innerWidth` / `window.innerHeight`. Use Canvas, WebGL, CSS animations — anything a modern browser supports.

```html
<!DOCTYPE html>
<html>
<head>
  <style>* { margin:0; } body { overflow:hidden; background:#000; }</style>
</head>
<body>
  <canvas id="c"></canvas>
  <script>
    // your wallpaper logic here
  </script>
</body>
</html>
```

---

## Project Structure

```
WallpaperPlatform/
├── App.xaml / App.xaml.cs          — application entry point
├── MainWindow.xaml / .cs           — full-screen WebView2 host, DPI-aware sizing
├── DesktopHelper.cs                — Win32 P/Invoke: WorkerW attachment
├── SystemTrayHelper.cs             — system tray icon and menu
├── WeatherBridge.cs                — periodic event bridge; posts snow/wind data to active wallpaper
└── wallpapers/
    ├── default/                    — built-in starfield wallpaper
    └── cabin-snow/                 — aurora cabin scene with layered snowfall, chimney smoke,
                                       flickering window glow, and twinkling stars
```

### Windows WorkerW Notes

The desktop layering trick differs between Windows versions:

- **Windows 11:** `WorkerW` is a direct child of `Progman`
- **Windows 10:** `WorkerW` is a top-level window below the icon layer

Both are handled automatically. A diagnostic file is written to `%TEMP%\WallpaperPlatform_diag.txt` on startup for troubleshooting.

---

## Event Bridge

Wallpapers receive live data from C# via `window.chrome.webview` message events. The active bridge posts a JSON payload on load and every 10 seconds:

```json
{ "type": "weather", "snow": 1.4, "wind": 0.8, "windX": -0.6 }
```

Listen in your wallpaper:

```javascript
window.chrome?.webview?.addEventListener('message', e => {
  const msg = JSON.parse(e.data);
  if (msg.type === 'weather') {
    // msg.snow  — intensity 0.3–3.0 (drives opacity + fall speed)
    // msg.wind  — strength 0.0–1.5
    // msg.windX — direction -1.0 (left) to 1.0 (right)
  }
});
```

Lerp toward received values each frame for smooth transitions. See `wallpapers/cabin-snow/index.html` for a full example.

---

## Paint-Anchor System

Wallpapers can pin effects to specific features of a background image regardless of monitor resolution. The system converts **image-pixel coordinates** (measured once against the original image) to screen coordinates at runtime using the same math as CSS `background-size: cover`.

```javascript
// In your wallpaper — define anchors as [imagePixelX, imagePixelY]
const ANCHORS = {
  chimneyTop: [901, 540],
  leftWindow: [766, 679],
};

// At runtime — resolves to the correct screen position on any monitor
const { x, y } = paintToCanvas(...ANCHORS.chimneyTop);
```

### Measuring anchors with debug mode

Append `?debug` to the wallpaper URL when opening it directly in a browser:

```
file:///path/to/wallpapers/my-wallpaper/index.html?debug
```

- **Left-click** anywhere on the image to drop a pin and record its image-pixel coordinates
- **Right-click** to clear all pins
- Captured coordinates are shown on-screen and logged to the browser console

Paste the logged values into your `ANCHORS` table. They will scale correctly to any screen size automatically.

---

## Roadmap

- [ ] Wallpaper picker UI  
- [ ] Windows startup registration  
- [ ] Multi-monitor support  
- [ ] License key validation for premium wallpaper packs  

---

## Contributing

Free wallpapers are welcome as pull requests. Add your package under `wallpapers/` with a `manifest.json` with `"tier": "free"`.

---

## License

Platform: [MIT](LICENSE)  
Premium wallpaper packages are distributed separately under commercial licenses.
