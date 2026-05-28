param([string]$OutPath = "$PSScriptRoot\..\app.ico")

Add-Type -AssemblyName System.Drawing

# Compile the drawing logic as C# to avoid PowerShell 5.1 struct/type quirks
Add-Type -ReferencedAssemblies @('System.Drawing') -TypeDefinition @'
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;

public static class IconGen
{
    public static void Generate(string outPath)
    {
        int[] sizes = { 16, 32, 48, 256 };
        var pngs = new byte[sizes.Length][];

        for (int i = 0; i < sizes.Length; i++)
            pngs[i] = RenderPng(sizes[i]);

        WriteIco(outPath, sizes, pngs);
    }

    static byte[] RenderPng(int size)
    {
        using var bmp = new Bitmap(size, size, PixelFormat.Format32bppArgb);
        using var g   = Graphics.FromImage(bmp);
        g.SmoothingMode    = SmoothingMode.AntiAlias;
        g.PixelOffsetMode  = PixelOffsetMode.HighQuality;

        float s = size;

        // Sky gradient
        using (var bg = new LinearGradientBrush(
            new Rectangle(0, 0, size, size),
            Color.FromArgb(255,  6, 10, 26),
            Color.FromArgb(255, 14, 28, 58),
            LinearGradientMode.Vertical))
            g.FillRectangle(bg, 0, 0, size, size);

        // Aurora band (32+)
        if (size >= 32)
        {
            int ah = (int)(s * 0.55f);
            using var aurBrush = new LinearGradientBrush(
                new Rectangle(0, 0, size, ah),
                Color.FromArgb(40, 20, 190, 120),
                Color.FromArgb( 0, 20, 190, 120),
                LinearGradientMode.Vertical);
            g.FillRectangle(aurBrush, 0, 0, size, ah);
        }

        // Mountain polygon
        float lx = s * 0.28f, ly = s * 0.36f;
        float rx = s * 0.67f, ry = s * 0.48f;
        var mtnPts = new PointF[] {
            new PointF(0,         s),
            new PointF(0,         s * 0.82f),
            new PointF(lx,        ly),
            new PointF(s * 0.46f, s * 0.64f),
            new PointF(rx,        ry),
            new PointF(s,         s * 0.76f),
            new PointF(s,         s)
        };
        using (var mtnBrush = new SolidBrush(Color.FromArgb(255, 20, 34, 60)))
            g.FillPolygon(mtnBrush, mtnPts);

        // Snow caps
        using (var snowBrush = new SolidBrush(Color.FromArgb(230, 228, 238, 255)))
        {
            foreach (var peak in new[] { (lx, ly, 0.10f), (rx, ry, 0.075f) })
            {
                float cr = s * peak.Item3;
                var cap = new PointF[] {
                    new PointF(peak.Item1,        peak.Item2),
                    new PointF(peak.Item1 - cr,   peak.Item2 + cr * 1.8f),
                    new PointF(peak.Item1 + cr,   peak.Item2 + cr * 1.8f)
                };
                g.FillPolygon(snowBrush, cap);
            }
        }

        // Stars
        using (var starBrush = new SolidBrush(Color.FromArgb(210, 255, 255, 245)))
        {
            foreach (var star in new[] { (0.74f, 0.11f, 0.052f), (0.18f, 0.17f, 0.038f), (0.50f, 0.06f, 0.032f) })
            {
                float sx = s * star.Item1, sy = s * star.Item2;
                float sr = Math.Max(0.8f, s * star.Item3);
                g.FillEllipse(starBrush, sx - sr, sy - sr, sr * 2, sr * 2);
            }
        }

        using var ms = new MemoryStream();
        bmp.Save(ms, ImageFormat.Png);
        return ms.ToArray();
    }

    static void WriteIco(string path, int[] sizes, byte[][] pngs)
    {
        using var writer = new BinaryWriter(File.Create(path));

        // ICONDIR
        writer.Write((ushort)0);
        writer.Write((ushort)1);
        writer.Write((ushort)sizes.Length);

        // ICONDIRENTRY x N
        uint offset = (uint)(6 + 16 * sizes.Length);
        for (int i = 0; i < sizes.Length; i++)
        {
            byte disp = sizes[i] >= 256 ? (byte)0 : (byte)sizes[i];
            writer.Write(disp);
            writer.Write(disp);
            writer.Write((byte)0);
            writer.Write((byte)0);
            writer.Write((ushort)1);
            writer.Write((ushort)32);
            writer.Write((uint)pngs[i].Length);
            writer.Write(offset);
            offset += (uint)pngs[i].Length;
        }

        // PNG payloads
        foreach (var png in pngs)
            writer.Write(png);
    }
}
'@

$out = [System.IO.Path]::GetFullPath($OutPath)
[IconGen]::Generate($out)
$kb = [Math]::Round((Get-Item $out).Length / 1KB, 1)
Write-Host "Done: $out  ($kb KB)"
