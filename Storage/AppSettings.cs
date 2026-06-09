namespace DeskVerse;

internal sealed record AppSettings(
    SentenceSource Source = SentenceSource.Hitokoto,
    string? JinrishiciToken = null,
    int RefreshMinutes = 30,
    WidgetPosition Position = WidgetPosition.TopCenter,
    FontSizeMode FontSize = FontSizeMode.Medium)
{
    public static AppSettings Load()
    {
        try
        {
            if (!File.Exists(DeskVersePaths.SettingsFile))
            {
                return new AppSettings();
            }

            var json = File.ReadAllText(DeskVersePaths.SettingsFile);
            return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
        }
        catch (Exception exception)
        {
            AppLogger.Log(exception, "Failed to load settings");
            return new AppSettings();
        }
    }

    public static void Save(AppSettings settings)
    {
        Directory.CreateDirectory(DeskVersePaths.AppDataDirectory);
        var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(DeskVersePaths.SettingsFile, json);
    }
}
