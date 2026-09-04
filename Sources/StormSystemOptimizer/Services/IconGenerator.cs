using System;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace StormSystemOptimizer.Services
{
    public static class IconGenerator
    {
        public static readonly int[] StandardIconSizes = { 256, 128, 64, 48, 32, 16 };

        public static Geometry? GetGeometryFromKey(string geometryKey)
        {
            try
            {
                if (Application.Current != null && Application.Current.TryFindResource(geometryKey) is Geometry geo)
                {
                    return geo.Clone();
                }
            }
            catch { }

            // Fallback standard geometries
            return geometryKey switch
            {
                "GeoFolder" or "GeoFolders" => Geometry.Parse("M10 4H4c-1.1 0-1.99.9-1.99 2L2 18c0 1.1.9 2 2 2h16c1.1 0 2-.9 2-2V8c0-1.1-.9-2-2-2h-8l-2-2z"),
                "GeoThisPC" => Geometry.Parse("M20 18c1.1 0 1.99-.9 1.99-2L22 6c0-1.1-.9-2-2-2H4c-1.1 0-2 .9-2 2v10c0 1.1.9 2 2 2H0v2h24v-2h-4zM4 6h16v10H4V6z"),
                "GeoRecycleBinEmpty" or "GeoTrash" => Geometry.Parse("M16 9v10H8V9h8m-1.5-6h-5l-1 1H5v2h14V4h-3.5l-1-1zM18 7H6v12c0 1.1.9 2 2 2h8c1.1 0 2-.9 2-2V7z"),
                "GeoRecycleBinFull" => Geometry.Parse("M16 9v10H8V9h8m-1.5-6h-5l-1 1H5v2h14V4h-3.5l-1-1zM18 7H6v12c0 1.1.9 2 2 2h8c1.1 0 2-.9 2-2V7zm-7 3h2v8h-2zm-3 0h2v8H8zm6 0h2v8h-2z"),
                "GeoDisks" or "GeoLocalDrive" => Geometry.Parse("M4 5h16a2 2 0 0 1 2 2v10a2 2 0 0 1-2 2H4a2 2 0 0 1-2-2V7a2 2 0 0 1 2-2zm0 2v10h16V7H4zm12 7h2v2h-2v-2zm-4 0h2v2h-2v-2z"),
                "GeoUserProfile" => Geometry.Parse("M12 12c2.21 0 4-1.79 4-4s-1.79-4-4-4-4 1.79-4 4 1.79 4 4 4zm0 2c-2.67 0-8 1.34-8 4v2h16v-2c0-2.66-5.33-4-8-4z"),
                "GeoNetwork" => Geometry.Parse("M12 2C6.48 2 2 6.48 2 12s4.48 10 10 10 10-4.48 10-10S17.52 2 12 2zm-1 17.93c-3.95-.49-7-3.85-7-7.93 0-.62.08-1.21.21-1.79L9 15v1c0 1.1.9 2 2 2v1.93zm6.9-2.54c-.26-.81-1-1.39-1.9-1.39h-1v-3c0-.55-.45-1-1-1H8v-2h2c.55 0 1-.45 1-1V7h2c1.1 0 2-.9 2-2v-.41c2.93 1.19 5 4.06 5 7.41 0 2.08-.8 3.97-2.1 5.39z"),
                "GeoTerminal" => Geometry.Parse("M20 4H4c-1.1 0-2 .9-2 2v12c0 1.1.9 2 2 2h16c1.1 0 2-.9 2-2V6c0-1.1-.9-2-2-2zm0 14H4V8h16v10zm-2-1h-6v-2h6v2zM7.5 17l-1.41-1.41L8.67 13l-2.58-2.59L7.5 9l4 4-4 4z"),
                _ => Geometry.Parse("M10 4H4c-1.1 0-1.99.9-1.99 2L2 18c0 1.1.9 2 2 2h16c1.1 0 2-.9 2-2V8c0-1.1-.9-2-2-2h-8l-2-2z")
            };
        }

        public static (Color bgStart, Color bgEnd, Color border, Color iconStart, Color iconEnd) GetThemePalette(string themeName, string? customAccentHex = null)
        {
            if (themeName.Contains("STORM Cyber Glow", StringComparison.OrdinalIgnoreCase))
            {
                return (
                    Color.FromRgb(10, 14, 26),   // #0A0E1A
                    Color.FromRgb(15, 23, 42),   // #0F172A
                    Color.FromRgb(0, 210, 255),  // #00D2FF
                    Color.FromRgb(0, 240, 255),  // #00F0FF
                    Color.FromRgb(2, 132, 199)   // #0284C7
                );
            }
            if (themeName.Contains("Fluent", StringComparison.OrdinalIgnoreCase))
            {
                return (
                    Color.FromRgb(24, 24, 27),   // #18181B
                    Color.FromRgb(39, 39, 42),   // #27272A
                    Color.FromRgb(56, 189, 248), // #38BDF8
                    Color.FromRgb(56, 189, 248), // #38BDF8
                    Color.FromRgb(2, 132, 199)   // #0284C7
                );
            }
            if (themeName.Contains("Cyberpunk", StringComparison.OrdinalIgnoreCase))
            {
                return (
                    Color.FromRgb(15, 23, 42),   // #0F172A
                    Color.FromRgb(20, 10, 30),   // Dark purple
                    Color.FromRgb(250, 204, 21), // #FACC15
                    Color.FromRgb(254, 224, 71), // #FDE047
                    Color.FromRgb(245, 158, 11)  // #F59E0B
                );
            }
            if (themeName.Contains("Monochrome", StringComparison.OrdinalIgnoreCase))
            {
                return (
                    Color.FromRgb(9, 9, 11),     // #09090B
                    Color.FromRgb(24, 24, 27),   // #18181B
                    Color.FromRgb(226, 232, 240),// #E2E8F0
                    Color.FromRgb(248, 250, 252),// #F8FAFC
                    Color.FromRgb(203, 213, 225) // #CBD5E1
                );
            }
            if (themeName.Contains("macOS", StringComparison.OrdinalIgnoreCase) || themeName.Contains("Tahoe", StringComparison.OrdinalIgnoreCase))
            {
                return (
                    Color.FromRgb(30, 27, 75),   // #1E1B4B
                    Color.FromRgb(21, 17, 43),   // #15112B
                    Color.FromRgb(168, 85, 247), // #A855F7
                    Color.FromRgb(192, 132, 252),// #C084FC
                    Color.FromRgb(147, 51, 234)  // #9333EA
                );
            }

            // Custom or default
            Color accent = Color.FromRgb(0, 210, 255);
            if (!string.IsNullOrEmpty(customAccentHex))
            {
                try { accent = (Color)ColorConverter.ConvertFromString(customAccentHex); } catch { }
            }

            return (
                Color.FromRgb(15, 23, 42),
                Color.FromRgb(10, 14, 26),
                accent,
                accent,
                Color.FromRgb(2, 132, 199)
            );
        }

        public static RenderTargetBitmap RenderIconFrame(Geometry geometry, int size, string themeName, string? customAccentHex = null)
        {
            var palette = GetThemePalette(themeName, customAccentHex);
            var visual = new DrawingVisual();

            using (var dc = visual.RenderOpen())
            {
                double cornerRadius = size * 0.22;
                double strokeWidth = Math.Max(1.0, size / 32.0);
                var bgRect = new Rect(strokeWidth / 2.0, strokeWidth / 2.0, size - strokeWidth, size - strokeWidth);

                // Background gradient
                var bgBrush = new LinearGradientBrush(palette.bgStart, palette.bgEnd, new Point(0, 0), new Point(1, 1));
                var borderPen = new Pen(new SolidColorBrush(palette.border), strokeWidth);
                dc.DrawRoundedRectangle(bgBrush, borderPen, bgRect, cornerRadius, cornerRadius);

                // Icon glyph
                var iconBrush = new LinearGradientBrush(palette.iconStart, palette.iconEnd, new Point(0, 0), new Point(1, 1));
                double pad = size * 0.24;
                double iconSize = size - pad * 2.0;

                var geoCopy = geometry.Clone();
                Rect bounds = geoCopy.Bounds;
                if (bounds.Width > 0 && bounds.Height > 0)
                {
                    double maxDim = Math.Max(bounds.Width, bounds.Height);
                    double scale = iconSize / maxDim;

                    var transform = new TransformGroup();
                    transform.Children.Add(new TranslateTransform(-bounds.Left, -bounds.Top));
                    transform.Children.Add(new ScaleTransform(scale, scale));
                    double offsetX = pad + (iconSize - bounds.Width * scale) / 2.0;
                    double offsetY = pad + (iconSize - bounds.Height * scale) / 2.0;
                    transform.Children.Add(new TranslateTransform(offsetX, offsetY));
                    geoCopy.Transform = transform;

                    dc.DrawGeometry(iconBrush, null, geoCopy);
                }
            }

            var rtb = new RenderTargetBitmap(size, size, 96, 96, PixelFormats.Pbgra32);
            rtb.Render(visual);
            return rtb;
        }

        public static byte[] RenderFrameToPngBytes(Geometry geometry, int size, string themeName, string? customAccentHex = null)
        {
            var rtb = RenderIconFrame(geometry, size, themeName, customAccentHex);
            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(rtb));
            using var ms = new MemoryStream();
            encoder.Save(ms);
            return ms.ToArray();
        }

        public static byte[] GenerateIcoBytes(Geometry geometry, string themeName, string? customAccentHex = null)
        {
            byte[][] pngImages = new byte[StandardIconSizes.Length][];
            for (int i = 0; i < StandardIconSizes.Length; i++)
            {
                pngImages[i] = RenderFrameToPngBytes(geometry, StandardIconSizes[i], themeName, customAccentHex);
            }

            using var ms = new MemoryStream();
            using (var bw = new BinaryWriter(ms))
            {
                bw.Write((ushort)0); // idReserved
                bw.Write((ushort)1); // idType (1 = ICO)
                bw.Write((ushort)StandardIconSizes.Length); // idCount

                int offset = 6 + (16 * StandardIconSizes.Length);
                for (int i = 0; i < StandardIconSizes.Length; i++)
                {
                    int size = StandardIconSizes[i];
                    bw.Write((byte)(size >= 256 ? 0 : size)); // bWidth
                    bw.Write((byte)(size >= 256 ? 0 : size)); // bHeight
                    bw.Write((byte)0); // bColorCount
                    bw.Write((byte)0); // bReserved
                    bw.Write((ushort)1); // wPlanes
                    bw.Write((ushort)32); // wBitCount
                    bw.Write((uint)pngImages[i].Length); // dwBytesInRes
                    bw.Write((uint)offset); // dwImageOffset

                    offset += pngImages[i].Length;
                }

                for (int i = 0; i < StandardIconSizes.Length; i++)
                {
                    bw.Write(pngImages[i]);
                }
            }

            return ms.ToArray();
        }

        public static void SaveGeometryToIcoFile(Geometry geometry, string outputPath, string themeName, string? customAccentHex = null)
        {
            string dir = Path.GetDirectoryName(outputPath)!;
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            byte[] bytes = GenerateIcoBytes(geometry, themeName, customAccentHex);
            File.WriteAllBytes(outputPath, bytes);
        }

        public static void SaveGeometryKeyToIcoFile(string geometryKey, string outputPath, string themeName, string? customAccentHex = null)
        {
            var geo = GetGeometryFromKey(geometryKey) ?? GetGeometryFromKey("GeoFolder")!;
            SaveGeometryToIcoFile(geo, outputPath, themeName, customAccentHex);
        }
    }
}
