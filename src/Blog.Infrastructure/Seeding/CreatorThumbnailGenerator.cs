using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace Blog.Infrastructure.Seeding;

internal static class CreatorThumbnailGenerator
{
    public const int Width = 1280;
    public const int Height = 720;

    public static Stream Render(string title, string badge, string accentHex, bool playButton)
    {
        var accent = Color.ParseHex(accentHex.TrimStart('#'));
        var bg = Color.ParseHex("0B1220");
        var bg2 = Color.ParseHex("152238");
        var ink = Color.White;
        var muted = Color.ParseHex("C7D2E0");

        var titleFont = LoadFont(88);
        var badgeFont = LoadFont(28);
        var brandFont = LoadFont(26);
        var decoFont = LoadFont(220);

        using var image = new Image<Rgba32>(Width, Height);
        image.Mutate(ctx =>
        {
            ctx.Fill(bg);
            ctx.Fill(bg2, new RectangularPolygon(0, 0, Width, 18));
            ctx.Fill(bg2, new RectangularPolygon(0, Height - 88, Width, 88));
            ctx.Fill(accent, new RectangularPolygon(0, 0, 22, Height));

            ctx.Fill(Color.FromRgba(255, 255, 255, 18), new RectangularPolygon(Width - 280, 0, 280, Height));

            ctx.DrawText(new RichTextOptions(decoFont)
            {
                Origin = new PointF(Width - 430, 40),
                HorizontalAlignment = HorizontalAlignment.Left
            }, "{}", Color.FromRgba(255, 255, 255, 18));

            var badgeBg = new RectangularPolygon(56, 48, MeasureBadgeWidth(badge, badgeFont), 52);
            ctx.Fill(accent, badgeBg);
            ctx.DrawText(new RichTextOptions(badgeFont)
            {
                Origin = new PointF(72, 58),
                HorizontalAlignment = HorizontalAlignment.Left
            }, badge.ToUpperInvariant(), ink);

            ctx.DrawText(new RichTextOptions(titleFont)
            {
                Origin = new PointF(56, 150),
                WrappingLength = 820,
                LineSpacing = 1.05f,
                HorizontalAlignment = HorizontalAlignment.Left
            }, title, ink);

            ctx.DrawText(new RichTextOptions(brandFont)
            {
                Origin = new PointF(56, Height - 54)
            }, "ABI HELPLINE  ·  BITNOCK  ·  MUHAMMAD BABAR", muted);

            if (playButton)
            {
                var cx = Width - 170f;
                var cy = Height / 2f + 10f;
                ctx.Fill(accent, new EllipsePolygon(cx, cy, 78));
                ctx.Fill(ink, new Polygon(new LinearLineSegment(
                    new PointF(cx - 18, cy - 32),
                    new PointF(cx - 18, cy + 32),
                    new PointF(cx + 38, cy))));
            }
            else
            {
                ctx.Fill(accent, new EllipsePolygon(Width - 140, Height - 170, 18));
                ctx.Fill(Color.FromRgba(255, 255, 255, 30), new EllipsePolygon(Width - 210, 120, 90));
            }
        });

        var output = new MemoryStream();
        image.SaveAsPng(output);
        output.Position = 0;
        return output;
    }

    private static float MeasureBadgeWidth(string badge, Font font)
    {
        var size = TextMeasurer.MeasureSize(badge.ToUpperInvariant(), new TextOptions(font));
        return Math.Max(140, size.Width + 40);
    }

    private static Font LoadFont(float size)
    {
        var collection = new FontCollection();
        foreach (var path in FontCandidates())
        {
            if (!File.Exists(path))
            {
                continue;
            }

            var family = collection.Add(path);
            return family.CreateFont(size, FontStyle.Bold);
        }

        return SystemFonts.CreateFont("Arial", size, FontStyle.Bold);
    }

    private static IEnumerable<string> FontCandidates() =>
    [
        @"C:\Windows\Fonts\impact.ttf",
        @"C:\Windows\Fonts\arialbd.ttf",
        @"C:\Windows\Fonts\segoeuib.ttf",
        @"C:\Windows\Fonts\calibrib.ttf",
        "/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf"
    ];
}
