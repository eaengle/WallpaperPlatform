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

    // Sky gradient: deep navy top → slightly lighter bottom
    using (var bg = new LinearGradientBrush(
        new Rectangle(0, 0, size, size),
        Color.FromArgb(255,  6, 10, 26),
        Color.FromArgb(255, 14, 28, 58),
        LinearGradientMode.Vertical))
        g.FillRectangle(bg, 0, 0, size, size);

    // Subtle aurora wash (32+ only)
    if (size >= 32)
    {
        int ah = (int)(s * 0.55f);
        using var aur = new LinearGradientBrush(
            new Rectangle(0, 0, size, ah),
            Color.FromArgb(40, 20, 190, 120),
            Color.FromArgb( 0, 20, 190, 120),
            LinearGradientMode.Vertical);
        g.FillRectangle(aur, 0, 0, size, ah);
    }

    // Mountain silhouette — left peak taller, right peak shorter
    float lx = s * 0.28f, ly = s * 0.36f;
    float rx = s * 0.67f, ry = s * 0.48f;
    PointF[] mtn =
    [
        new(0,         s),
        new(0,         s * 0.82f),
        new(lx,        ly),
        new(s * 0.46f, s * 0.64f),
        new(rx,        ry),
        new(s,         s * 0.76f),
        new(s,         s),
    ];
    using (var mb = new SolidBrush(Color.FromArgb(255, 20, 34, 60)))
        g.FillPolygon(mb, mtn);

    // Snow caps
    using (var snow = new SolidBrush(Color.FromArgb(230, 228, 238, 255)))
    {
        foreach (var (px, py, rPct) in new[] { (lx, ly, 0.10f), (rx, ry, 0.075f) })
        {
            float cr = s * rPct;
            g.FillPolygon(snow, new PointF[]
            {
                new(px,       py),
                new(px - cr,  py + cr * 1.8f),
                new(px + cr,  py + cr * 1.8f),
            });
        }
    }

    // Stars — (normX, normY, normRadius)
    using (var star = new SolidBrush(Color.FromArgb(210, 255, 255, 245)))
    {
        foreach (var (nx, ny, nr) in new[] { (0.74f, 0.11f, 0.052f), (0.18f, 0.17f, 0.038f), (0.50f, 0.06f, 0.032f) })
        {
            float sx = s * nx, sy = s * ny, sr = MathF.Max(0.8f, s * nr);
            g.FillEllipse(star, sx - sr, sy - sr, sr * 2, sr * 2);
        }
    }

    using var ms = new MemoryStream();
    bmp.Save(ms, ImageFormat.Png);
    return ms.ToArray();
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
