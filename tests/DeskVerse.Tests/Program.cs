using DeskVerse;

var tests = new (string Name, Action Run)[]
{
    ("Startup run command quotes executable path", StartupRunCommandQuotesExecutablePath),
    ("Startup path parser handles quoted command", StartupPathParserHandlesQuotedCommand),
    ("Startup path parser handles unquoted command", StartupPathParserHandlesUnquotedCommand),
    ("Wallpaper theme keeps text distinct from background", WallpaperThemeKeepsTextDistinctFromBackground),
    ("Animation easing stays in expected bounds", AnimationEasingStaysInExpectedBounds),
    ("Countdown remaining days clamps past targets", CountdownRemainingDaysClampsPastTargets)
};

var failures = new List<string>();
foreach (var test in tests)
{
    try
    {
        test.Run();
        Console.WriteLine($"PASS {test.Name}");
    }
    catch (Exception exception)
    {
        failures.Add($"{test.Name}: {exception.Message}");
        Console.WriteLine($"FAIL {test.Name}: {exception.Message}");
    }
}

if (failures.Count > 0)
{
    Console.WriteLine();
    Console.WriteLine("Failures:");
    foreach (var failure in failures)
    {
        Console.WriteLine($"- {failure}");
    }

    return 1;
}

return 0;

static void StartupRunCommandQuotesExecutablePath()
{
    var path = @"C:\Program Files\DeskVerse\DeskVerse.exe";
    AssertEqual("\"C:\\Program Files\\DeskVerse\\DeskVerse.exe\"", StartupManager.BuildRunCommand(path));
}

static void StartupPathParserHandlesQuotedCommand()
{
    var path = @"C:\Program Files\DeskVerse\DeskVerse.exe";
    AssertEqual(path, StartupManager.ExtractExecutablePath($"\"{path}\""));
}

static void StartupPathParserHandlesUnquotedCommand()
{
    AssertEqual(@"C:\Tools\DeskVerse.exe", StartupManager.ExtractExecutablePath(@"C:\Tools\DeskVerse.exe --quiet"));
}

static void WallpaperThemeKeepsTextDistinctFromBackground()
{
    var darkTheme = DesktopTheme.FromWallpaperColor(Color.FromArgb(18, 24, 32));
    var lightTheme = DesktopTheme.FromWallpaperColor(Color.FromArgb(230, 220, 198));

    AssertTrue(Contrast(darkTheme.Background, darkTheme.Text) > 3.0, "dark theme contrast is too low");
    AssertTrue(Contrast(lightTheme.Background, lightTheme.Text) > 3.0, "light theme contrast is too low");
}

static void AnimationEasingStaysInExpectedBounds()
{
    AssertEqual(0.0, HitokotoWidgetForm.EaseOutCubic(-1));
    AssertEqual(0.0, HitokotoWidgetForm.EaseOutCubic(0));
    AssertEqual(1.0, HitokotoWidgetForm.EaseOutCubic(1));
    AssertEqual(1.0, HitokotoWidgetForm.EaseOutCubic(2));
    AssertTrue(HitokotoWidgetForm.EaseOutCubic(0.5) is > 0.5 and < 1.0, "midpoint should ease forward");
}

static void CountdownRemainingDaysClampsPastTargets()
{
    var today = new DateTime(2026, 6, 9);

    AssertEqual(10, CountdownCalculator.RemainingDays(today, new DateTime(2026, 6, 19)));
    AssertEqual(0, CountdownCalculator.RemainingDays(today, new DateTime(2026, 6, 9)));
    AssertEqual(0, CountdownCalculator.RemainingDays(today, new DateTime(2026, 6, 1)));
}

static double Contrast(Color first, Color second)
{
    var a = RelativeLuminance(first) + 0.05;
    var b = RelativeLuminance(second) + 0.05;
    return Math.Max(a, b) / Math.Min(a, b);
}

static double RelativeLuminance(Color color)
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

static void AssertEqual<T>(T expected, T actual)
{
    if (!EqualityComparer<T>.Default.Equals(expected, actual))
    {
        throw new InvalidOperationException($"expected '{expected}', got '{actual}'");
    }
}

static void AssertTrue(bool condition, string message)
{
    if (!condition)
    {
        throw new InvalidOperationException(message);
    }
}
