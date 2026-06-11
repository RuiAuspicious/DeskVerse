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

        var neutralGlass = isDark
            ? Color.FromArgb(86, 91, 101)
            : Color.FromArgb(245, 248, 252);
        var surface = Blend(theme.Background, neutralGlass, isDark ? 0.52 + t * 0.12 : 0.62 + t * 0.1);
        var surfaceTop = Blend(surface, Color.White, isDark ? 0.13 + t * 0.08 : 0.1 + t * 0.06);
        var surfaceBottom = Blend(surface, Color.Black, isDark ? 0.035 + t * 0.025 : 0.018 + t * 0.012);
        var specular = Color.FromArgb((int)Math.Round(14 + 32 * t), Color.White);
        var edgeLight = Color.FromArgb((int)Math.Round(34 + 62 * t), Color.White);
        var edgeShade = Color.FromArgb((int)Math.Round(8 + 18 * t), Color.Black);
        var border = isDark
            ? Color.FromArgb((int)Math.Round(40 + 46 * t), Color.White)
            : Color.FromArgb((int)Math.Round(24 + 22 * t), Color.FromArgb(32, 37, 47));
        var innerBorder = Color.FromArgb((int)Math.Round(10 + 24 * t), Color.White);
        var text = isDark
            ? Color.FromArgb(248, 249, 247)
            : Color.FromArgb(22, 25, 30);
        var secondary = isDark
            ? Color.FromArgb(216, 220, 218)
            : Color.FromArgb(78, 84, 92);
        var opacity = 0.985 - t * 0.045;
        var radius = (int)Math.Round(22 + t * 8);

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
            text,
            secondary,
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
