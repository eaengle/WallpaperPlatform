# WallpaperPlatform

An open-source Windows desktop wallpaper host that renders animated, event-driven wallpapers using a WebView2 (Chromium) engine embedded behind your desktop icons.

Wallpapers are self-contained HTML/JS/WebGL packages — the same web skills you already have. The platform is free and open-source; premium wallpaper packs are the monetization layer.

---

## Features

- Renders wallpapers **behind desktop icons** using the Windows `WorkerW` layer
- Full **4K / multi-DPI** support
- Wallpapers are plain **HTML + JavaScript** — use Canvas, WebGL, Three.js, anything
- **System tray** control — switch scenes, trigger events manually, or exit cleanly
- Wallpaper packages defined by a simple `manifest.json`
- **Weather bridge** — live snow/wind data posted to the active wallpaper every 10 seconds
- **Scene event bridge** — C# fires named events on random schedules; wallpapers react with visual effects
- **Paint-anchor system** — pin effects to image-pixel coordinates; they scale to any monitor automatically

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

## System Tray

Right-clicking the tray icon shows:

```
Select Scene   ▶
    ✓ Cabin in Snow
      Default
──────────────────
Trigger Event  ▶
    Shooting Star
    Blizzard Surge
    Cabin Flicker
    Window Shadow
    Owl Swoop
──────────────────
Exit
```

**Select Scene** lists every wallpaper found in the `wallpapers/` directory. Display names come from each package's `manifest.json`. The active scene is ticked. Selecting an entry switches the wallpaper immediately with no restart required.

**Trigger Event** fires any scene event immediately — useful for testing or for users who want to see an effect on demand. Events are otherwise fired automatically by the C# event bridge on random schedules.

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
  "tier": "free",
  "events": [
    { "name": "my_event", "label": "My Event", "minSeconds": 60, "maxSeconds": 300 }
  ]
}
```

| Field | Values |
|---|---|
| `tier` | `"free"` or `"premium"` |

#### events

The optional `events` array declares which scene events this wallpaper supports. The platform reads this at load time to:

- Schedule automatic event firing via `WallpaperEventBridge` (using `minSeconds`/`maxSeconds` as the random interval window)
- Populate the **Trigger Event** submenu in the system tray with only the events relevant to the active scene
- Disable **Trigger Event** entirely when the active wallpaper declares no events

| Field | Description |
|---|---|
| `name` | Internal event name posted to the wallpaper JS |
| `label` | Display name shown in the system tray menu |
| `minSeconds` | Minimum random delay between automatic firings |
| `maxSeconds` | Maximum random delay between automatic firings |

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
├── WeatherBridge.cs                — periodic weather data bridge (snow, wind)
├── WallpaperEventBridge.cs         — C#-scheduled scene event bridge
├── app.ico                         — application icon (regenerate via tools/IconGen)
├── tools/
│   └── IconGen/                    — .NET console tool that generates app.ico
└── wallpapers/
    ├── default/                    — built-in starfield wallpaper
    └── cabin-snow/                 — aurora cabin scene with layered snowfall, chimney smoke,
                                       flickering window glow, twinkling stars, window shadow, owl swoop, and rabbit hop events
```

### Windows WorkerW Notes

The desktop layering trick differs between Windows versions:

- **Windows 11:** `WorkerW` is a direct child of `Progman`
- **Windows 10:** `WorkerW` is a top-level window below the icon layer

Both are handled automatically. A diagnostic file is written to `%TEMP%\WallpaperPlatform_diag.txt` on startup for troubleshooting.

---

## Message Bridge

Wallpapers receive data from C# via `window.chrome.webview` message events. Two message types are in use.

### Weather data

Posted on load and every 10 seconds by `WeatherBridge`:

```json
{ "type": "weather", "snow": 1.4, "wind": 0.8, "windX": -0.6 }
```

| Field | Range | Description |
|---|---|---|
| `snow` | 0.3 – 3.0 | Snowfall intensity (opacity + fall speed) |
| `wind` | 0.0 – 1.5 | Wind strength |
| `windX` | -1.0 – 1.0 | Wind direction (negative = left) |

### Scene events

Posted by `WallpaperEventBridge` on randomised C# timers, or immediately when triggered from the system tray:

```json
{ "type": "event", "name": "shooting_star", "data": null }
```

The `data` field is optional and event-specific. Built-in events for the cabin-snow scene (intervals configured in `wallpapers/cabin-snow/manifest.json`):

| Event | Effect |
|---|---|
| `shooting_star` | A streak of light crosses the sky |
| `blizzard_surge` | Snow and wind spike for a short burst |
| `cabin_flicker` | Window lights stutter as if losing power |
| `window_shadow` | An antlered silhouette appears briefly in a cabin window |
| `owl_swoop` | An owl swoops in from the left, perches on the tree, then departs |
| `rabbit_hop` | A rabbit hops in from one side, pauses, then hops out the other side past the boulder |

### Listening in your wallpaper

```javascript
window.chrome?.webview?.addEventListener('message', e => {
  const msg = JSON.parse(e.data);

  if (msg.type === 'weather') {
    // update targetSnow, targetWindX, targetWindStr
    // lerp toward them each frame for smooth transitions
  } else if (msg.type === 'event') {
    handleSceneEvent(msg.name, msg.data);
  }
});
```

See `wallpapers/cabin-snow/index.html` for a complete implementation.

### Adding events in C#

Register events in `MainWindow.OnNavigationCompleted` by passing `EventDef` records to `WallpaperEventBridge`:

```csharp
_events = new WallpaperEventBridge(WebView,
[
    new WallpaperEventBridge.EventDef("my_event", TimeSpan.FromMinutes(2), TimeSpan.FromMinutes(10)),
]);
_events.Start();
```

You can also fire any event immediately from C# at any time:

```csharp
_events.PostEvent("my_event");
```

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

## Regenerating the Icon

The app icon is generated by a small .NET tool:

```bash
dotnet run --project tools/IconGen -- path/to/app.ico
```

Edit `tools/IconGen/Program.cs` to change the design, then rebuild the main project to pick up the new `app.ico`.

---

## Roadmap

- [x] Scene switcher via system tray  
- [ ] Windows startup registration  
- [ ] Multi-monitor support  
- [ ] License key validation for premium wallpaper packs
- [x] Event definitions driven by `manifest.json` (per-wallpaper event schedules)

---

## Contributing

Free wallpapers are welcome as pull requests. Add your package under `wallpapers/` with a `manifest.json` with `"tier": "free"`.

---

## License

Platform: [MIT](LICENSE)  
Premium wallpaper packages are distributed separately under commercial licenses.
