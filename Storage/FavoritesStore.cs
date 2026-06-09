namespace DeskVerse;

internal static class FavoritesStore
{
    public static void Add(HitokotoSentence sentence)
    {
        var favorites = Load();
        if (favorites.Any(item =>
                item.Text == sentence.Text &&
                item.From == sentence.From &&
                item.FromWho == sentence.FromWho &&
                item.Source == sentence.Source))
        {
            return;
        }

        favorites.Insert(0, FavoriteSentence.FromSentence(sentence));
        Save(favorites);
    }

    public static void OpenFavoritesFile()
    {
        Directory.CreateDirectory(DeskVersePaths.AppDataDirectory);
        if (!File.Exists(DeskVersePaths.FavoritesFile))
        {
            Save([]);
        }

        Process.Start(new ProcessStartInfo(DeskVersePaths.FavoritesFile)
        {
            UseShellExecute = true
        });
    }

    private static List<FavoriteSentence> Load()
    {
        try
        {
            if (!File.Exists(DeskVersePaths.FavoritesFile))
            {
                return [];
            }

            var json = File.ReadAllText(DeskVersePaths.FavoritesFile);
            return JsonSerializer.Deserialize<List<FavoriteSentence>>(json) ?? [];
        }
        catch (Exception exception)
        {
            AppLogger.Log(exception, "Failed to load favorites");
            return [];
        }
    }

    private static void Save(List<FavoriteSentence> favorites)
    {
        Directory.CreateDirectory(DeskVersePaths.AppDataDirectory);
        var json = JsonSerializer.Serialize(favorites, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(DeskVersePaths.FavoritesFile, json);
    }
}
