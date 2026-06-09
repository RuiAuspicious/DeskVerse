namespace DeskVerse;

internal sealed record DesktopTheme(Color Background, Color Text, Color SecondaryText, Color Border, double Opacity)
{
    public static DesktopTheme FromWallpaperColor(Color color)
    {
        var muted = Muted(color);
        var luminance = RelativeLuminance(muted);
        var isDark = luminance < 0.48;

        var background = isDark
            ? Blend(muted, Color.FromArgb(48, 50, 56), 0.2)
            : Blend(muted, Color.FromArgb(244, 247, 246), 0.36);
        var text = isDark
            ? Color.FromArgb(239, 242, 241)
            : Color.FromArgb(26, 31, 39);
        var secondary = isDark
            ? Color.FromArgb(190, 198, 198)
            : Color.FromArgb(82, 94, 94);
        var border = isDark
            ? Color.FromArgb(64, 255, 255, 255)
            : Color.FromArgb(72, 32, 37, 47);

        return new DesktopTheme(background, text, secondary, border, 0.86);
    }

    private static Color Muted(Color color)
    {
        var gray = (color.R + color.G + color.B) / 3;
        return Color.FromArgb(
            BlendChannel(color.R, gray, 0.34),
            BlendChannel(color.G, gray, 0.34),
            BlendChannel(color.B, gray, 0.34));
    }

    private static Color Blend(Color first, Color second, double amount)
    {
        return Color.FromArgb(
            BlendChannel(first.R, second.R, amount),
            BlendChannel(first.G, second.G, amount),
            BlendChannel(first.B, second.B, amount));
    }

    private static int BlendChannel(int first, int second, double amount)
    {
        return (int)Math.Round(first + (second - first) * amount);
    }

    private static double RelativeLuminance(Color color)
    {
        static double Convert(int value)
        {
            var channel = value / 255.0;
            return channel <= 0.03928
                ? channel / 12.92
                : Math.Pow((channel + 0.055) / 1.055, 2.4);
        }

        return Convert(color.R) * 0.2126 + Convert(color.G) * 0.7152 + Convert(color.B) * 0.0722;
    }
}
