namespace DeskVerse;

internal sealed record LiquidGlassMaterial(
    int Intensity,
    Color Surface,
    Color SurfaceTop,
    Color SurfaceBottom,
    Color Specular,
    Color EdgeLight,
    Color EdgeShade,
    Color Border,
    Color InnerBorder,
    Color Text,
    Color SecondaryText,
    double WindowOpacity,
    int Radius)
{
    public static int NormalizeIntensity(int intensity)
    {
        return Math.Clamp(intensity, 0, 100);
    }

    public static LiquidGlassMaterial FromTheme(DesktopTheme theme, int intensity)
    {
        var normalized = NormalizeIntensity(intensity);
        var t = normalized / 100.0;
        var isDark = RelativeLuminance(theme.Background) < 0.5;

        var glassTint = isDark
            ? Blend(theme.Background, Color.FromArgb(76, 84, 99), 0.32 + t * 0.2)
            : Blend(theme.Background, Color.FromArgb(250, 253, 255), 0.26 + t * 0.28);
        var surface = Blend(theme.Background, glassTint, 0.44 + t * 0.38);
        var surfaceTop = Blend(surface, Color.White, isDark ? 0.1 + t * 0.14 : 0.16 + t * 0.18);
        var surfaceBottom = Blend(surface, Color.Black, isDark ? 0.12 + t * 0.1 : 0.05 + t * 0.06);
        var specular = Color.FromArgb((int)Math.Round(24 + 92 * t), Color.White);
        var edgeLight = Color.FromArgb((int)Math.Round(44 + 108 * t), Color.White);
        var edgeShade = Color.FromArgb((int)Math.Round(22 + 58 * t), Color.Black);
        var border = isDark
            ? Color.FromArgb((int)Math.Round(54 + 92 * t), Color.White)
            : Color.FromArgb((int)Math.Round(48 + 58 * t), Color.FromArgb(32, 37, 47));
        var innerBorder = Color.FromArgb((int)Math.Round(14 + 48 * t), Color.White);
        var opacity = 0.94 - t * 0.08;
        var radius = (int)Math.Round(14 + t * 10);

        return new LiquidGlassMaterial(
            normalized,
            surface,
            surfaceTop,
            surfaceBottom,
            specular,
            edgeLight,
            edgeShade,
            border,
            innerBorder,
            theme.Text,
            theme.SecondaryText,
            opacity,
            radius);
    }

    internal static Color Blend(Color first, Color second, double amount)
    {
        amount = Math.Clamp(amount, 0, 1);
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
