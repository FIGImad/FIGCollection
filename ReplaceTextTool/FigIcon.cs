using System.Reflection;

namespace ReplaceTextTool;

/// <summary>
/// Builds the application icon from the embedded FIG Logo with a replace-text badge overlay.
/// </summary>
internal static class FigIcon
{
    /// <summary>
    /// Loads the embedded FIG Logo icon and overlays a small "A→B" replace-text badge
    /// in the bottom-right corner to identify this as the ReplaceTextTool.
    /// </summary>
    public static Icon Create(int size = 32)
    {
        using var baseIcon = LoadEmbeddedIcon(size);
        using var bitmap = new Bitmap(size, size);
        using var g = Graphics.FromImage(bitmap);

        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
        g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;

        // Draw the base FIG logo
        g.DrawIcon(baseIcon, new Rectangle(0, 0, size, size));

        // Badge dimensions – bottom-right quarter of the icon
        int badgeSize = (int)Math.Round(size * 0.52);
        int bx = size - badgeSize;
        int by = size - badgeSize;

        // Semi-transparent dark pill background for the badge
        using var bgBrush = new SolidBrush(Color.FromArgb(210, 12, 28, 60));
        int radius = badgeSize / 4;
        FillRoundedRectangle(g, bgBrush, new Rectangle(bx, by, badgeSize, badgeSize), radius);

        // "A→B" text drawn in two lines inside the badge
        float labelSize = badgeSize * 0.30f;
        using var labelFont = new Font("Segoe UI", labelSize, FontStyle.Bold, GraphicsUnit.Pixel);
        using var arrowFont = new Font("Segoe UI", labelSize * 0.85f, FontStyle.Regular, GraphicsUnit.Pixel);

        using var goldBrush = new SolidBrush(Color.FromArgb(255, 220, 175, 40));
        using var whiteBrush = new SolidBrush(Color.White);

        // Line 1: "A" (source text, gold)
        string srcLabel = "A";
        SizeF srcSize = g.MeasureString(srcLabel, labelFont);
        g.DrawString(srcLabel, labelFont, goldBrush,
            bx + (badgeSize - srcSize.Width) / 2f,
            by + badgeSize * 0.06f);

        // Arrow "→" (white, slightly smaller)
        string arrow = "→";
        SizeF arrowMeasure = g.MeasureString(arrow, arrowFont);
        g.DrawString(arrow, arrowFont, whiteBrush,
            bx + (badgeSize - arrowMeasure.Width) / 2f,
            by + badgeSize * 0.34f);

        // Line 3: "B" (result text, gold)
        string dstLabel = "B";
        SizeF dstSize = g.MeasureString(dstLabel, labelFont);
        g.DrawString(dstLabel, labelFont, goldBrush,
            bx + (badgeSize - dstSize.Width) / 2f,
            by + badgeSize * 0.62f);

        return Icon.FromHandle(bitmap.GetHicon());
    }

    private static Icon LoadEmbeddedIcon(int size)
    {
        var assembly = Assembly.GetExecutingAssembly();
        // The resource name reflects the default namespace + filename (spaces become underscores in manifest)
        string resourceName = assembly.GetManifestResourceNames()
            .FirstOrDefault(n => n.Contains("FIG") && n.EndsWith(".ico"))
            ?? throw new InvalidOperationException("Embedded FIG Logo icon resource not found.");

        using var stream = assembly.GetManifestResourceStream(resourceName)!;
        return new Icon(stream, size, size);
    }

    private static void FillRoundedRectangle(Graphics g, Brush brush, Rectangle rect, int radius)
    {
        using var path = RoundedPath(rect, radius);
        g.FillPath(brush, path);
    }

    private static System.Drawing.Drawing2D.GraphicsPath RoundedPath(Rectangle r, int radius)
    {
        int d = radius * 2;
        var path = new System.Drawing.Drawing2D.GraphicsPath();
        path.AddArc(r.X, r.Y, d, d, 180, 90);
        path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
        path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
        path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
        path.CloseFigure();
        return path;
    }
}
