namespace DeskVerse;

internal sealed record AppSettings(
    SentenceSource Source = SentenceSource.Hitokoto,
    string? JinrishiciToken = null,
    int RefreshMinutes = 30,
    WidgetPosition Position = WidgetPosition.TopCenter,
    FontSizeMode FontSize = FontSizeMode.Medium,
    bool CountdownEnabled = false,
    string CountdownTitle = "目标日",
    DateTime CountdownTargetDate = default,
    string CountdownSubtitle = "把重要的日子放在桌面上",
    int GlassIntensity = 68)
{
    public DateTime EffectiveCountdownTargetDate()
    {
        return CountdownTargetDate == default
            ? DateTime.Today.AddDays(30)
            : CountdownTargetDate.Date;
    }

    public AppSettings Normalize()
    {
        return this with
        {
            CountdownTitle = string.IsNullOrWhiteSpace(CountdownTitle) ? "目标日" : CountdownTitle.Trim(),
            CountdownSubtitle = string.IsNullOrWhiteSpace(CountdownSubtitle) ? "把重要的日子放在桌面上" : CountdownSubtitle.Trim(),
            CountdownTargetDate = EffectiveCountdownTargetDate(),
            GlassIntensity = LiquidGlassMaterial.NormalizeIntensity(GlassIntensity)
        };
    }

    public static AppSettings Load()
    {
        try
        {
            if (!File.Exists(DeskVersePaths.SettingsFile))
            {
                return new AppSettings().Normalize();
            }

            var json = File.ReadAllText(DeskVersePaths.SettingsFile);
            return (JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings()).Normalize();
        }
        catch (Exception exception)
        {
            AppLogger.Log(exception, "Failed to load settings");
            return new AppSettings().Normalize();
        }
    }

    public static void Save(AppSettings settings)
    {
        Directory.CreateDirectory(DeskVersePaths.AppDataDirectory);
        var json = JsonSerializer.Serialize(settings.Normalize(), new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(DeskVersePaths.SettingsFile, json);
    }
}
