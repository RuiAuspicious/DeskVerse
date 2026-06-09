namespace DeskVerse;

internal static class WallpaperThemeReader
{
    private const int SpiGetDeskWallpaper = 0x0073;
    private const int MaxPath = 260;

    public static Color? TryReadTopCenterColor()
    {
        var path = GetWallpaperPath();
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
        {
            return null;
        }

        try
        {
            using var image = new Bitmap(path);
            if (image.Width <= 0 || image.Height <= 0)
            {
                return null;
            }

            var sample = GetTopCenterSampleArea(image);
            return AverageColor(image, sample);
        }
        catch (Exception exception)
        {
            AppLogger.Log(exception, "Failed to read wallpaper color");
            return null;
        }
    }

    private static string? GetWallpaperPath()
    {
        var builder = new StringBuilder(MaxPath);
        return SystemParametersInfo(SpiGetDeskWallpaper, builder.Capacity, builder, 0)
            ? builder.ToString()
            : null;
    }

    private static Rectangle GetTopCenterSampleArea(Bitmap image)
    {
        var width = Math.Max(1, (int)(image.Width * 0.58));
        var height = Math.Max(1, (int)(image.Height * 0.16));
        var x = Math.Max(0, (image.Width - width) / 2);
        var y = Math.Max(0, (int)(image.Height * 0.03));
        return new Rectangle(x, y, Math.Min(width, image.Width - x), Math.Min(height, image.Height - y));
    }

    private static Color AverageColor(Bitmap image, Rectangle area)
    {
        long red = 0;
        long green = 0;
        long blue = 0;
        long count = 0;
        var step = Math.Max(1, Math.Min(area.Width, area.Height) / 72);

        for (var y = area.Top; y < area.Bottom; y += step)
        {
            for (var x = area.Left; x < area.Right; x += step)
            {
                var pixel = image.GetPixel(x, y);
                red += pixel.R;
                green += pixel.G;
                blue += pixel.B;
                count++;
            }
        }

        return count == 0
            ? Color.FromArgb(38, 43, 52)
            : Color.FromArgb((int)(red / count), (int)(green / count), (int)(blue / count));
    }

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool SystemParametersInfo(int action, int parameter, StringBuilder value, int winIni);
}
