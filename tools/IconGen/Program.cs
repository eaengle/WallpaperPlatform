using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

string outPath = args.Length > 0 ? args[0]
    : Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "app.ico");
outPath = Path.GetFullPath(outPath);

int[] sizes = [16, 32, 48, 256];
var   pngs  = sizes.Select(RenderPng).ToArray();
WriteIco(outPath, sizes, pngs);

var kb = new FileInfo(outPath).Length / 1024.0;
Console.WriteLine($"Done: {outPath}  ({kb:F1} KB)");

// ─────────────────────────────────────────────────────────────────────────────
static byte[] RenderPng(int size)
{
    using var bmp = new Bitmap(size, size, PixelFormat.Format32bppArgb);
    using var g   = Graphics.FromImage(bmp);
    g.SmoothingMode   = SmoothingMode.AntiAlias;
    g.PixelOffsetMode = PixelOffsetMode.HighQuality;

    float s = size;

    // Background — dark rounded rect
    float bgR = size >= 48 ? s * 0.16f : size >= 32 ? s * 0.12f : 0f;
    using (var bgBrush = new SolidBrush(Color.FromArgb(255, 16, 16, 22)))
        FillRoundedRect(g, bgBrush, 0, 0, s, s, bgR);

    // Grid layout
    float pad      = s * 0.09f;
    float gap      = size >= 32 ? s * 0.05f : s * 0.06f;
    float tileSize = (s - 2 * pad - gap) / 2f;
    float tileR    = size >= 48 ? tileSize * 0.20f : size >= 32 ? tileSize * 0.14f : 0f;

    (float x, float y)[] positions =
    [
        (pad,               pad),
        (pad + tileSize + gap, pad),
        (pad,               pad + tileSize + gap),
        (pad + tileSize + gap, pad + tileSize + gap),
    ];

    // sunset/amber · night/blue · forest/teal · space/violet
    (Color top, Color bot)[] tileColors =
    [
        (Color.FromArgb(255, 225, 100, 28), Color.FromArgb(255, 155, 48, 12)),
        (Color.FromArgb(255,  28,  80, 170), Color.FromArgb(255,  10,  38,  95)),
        (Color.FromArgb(255,  22, 135,  78), Color.FromArgb(255,   8,  72,  42)),
        (Color.FromArgb(255,  90,  22, 158), Color.FromArgb(255,  42,   8,  95)),
    ];

    for (int i = 0; i < 4; i++)
    {
        var (tx, ty) = positions[i];
        var rect = new RectangleF(tx, ty, tileSize, tileSize);
        using var grad = new LinearGradientBrush(rect, tileColors[i].top, tileColors[i].bot, LinearGradientMode.Vertical);
        FillRoundedRect(g, grad, tx, ty, tileSize, tileSize, tileR);
    }

    using var ms = new MemoryStream();
    bmp.Save(ms, ImageFormat.Png);
    return ms.ToArray();
}

static void FillRoundedRect(Graphics g, Brush brush, float x, float y, float w, float h, float r)
{
    if (r <= 0)
    {
        g.FillRectangle(brush, x, y, w, h);
        return;
    }
    using var path = new GraphicsPath();
    float d = r * 2;
    path.AddArc(x,         y,         d, d, 180, 90);
    path.AddArc(x + w - d, y,         d, d, 270, 90);
    path.AddArc(x + w - d, y + h - d, d, d,   0, 90);
    path.AddArc(x,         y + h - d, d, d,  90, 90);
    path.CloseFigure();
    g.FillPath(brush, path);
}

// ─────────────────────────────────────────────────────────────────────────────
static void WriteIco(string path, int[] sizes, byte[][] pngs)
{
    using var w = new BinaryWriter(File.Create(path));

    // ICONDIR
    w.Write((ushort)0);
    w.Write((ushort)1);
    w.Write((ushort)sizes.Length);

    // ICONDIRENTRY × N
    uint offset = (uint)(6 + 16 * sizes.Length);
    for (int i = 0; i < sizes.Length; i++)
    {
        byte d = sizes[i] >= 256 ? (byte)0 : (byte)sizes[i];
        w.Write(d);                        // width  (0 = 256)
        w.Write(d);                        // height
        w.Write((byte)0);                  // color count
        w.Write((byte)0);                  // reserved
        w.Write((ushort)1);                // planes
        w.Write((ushort)32);               // bpp
        w.Write((uint)pngs[i].Length);     // image byte count
        w.Write(offset);                   // image data offset
        offset += (uint)pngs[i].Length;
    }

    foreach (var png in pngs)
        w.Write(png);
}
