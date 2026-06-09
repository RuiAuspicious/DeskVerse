namespace DeskVerse;

internal static class DeskVersePaths
{
    public static string AppDataDirectory { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "DeskVerse");

    public static string SettingsFile { get; } = Path.Combine(AppDataDirectory, "settings.json");

    public static string FavoritesFile { get; } = Path.Combine(AppDataDirectory, "favorites.json");

    public static string LogsDirectory { get; } = Path.Combine(AppDataDirectory, "logs");
}
