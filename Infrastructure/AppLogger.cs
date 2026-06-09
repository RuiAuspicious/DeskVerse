namespace DeskVerse;

internal static class AppLogger
{
    public static void Log(Exception exception, string context)
    {
        try
        {
            Directory.CreateDirectory(DeskVersePaths.LogsDirectory);
            var path = Path.Combine(DeskVersePaths.LogsDirectory, $"{DateTime.Now:yyyy-MM-dd}.log");
            var message = $"""
                [{DateTimeOffset.Now:O}] {context}
                {exception}

                """;
            File.AppendAllText(path, message);
        }
        catch
        {
            // Logging must never make the desktop widget noisier or less reliable.
        }
    }

    public static void LogMessage(string message)
    {
        try
        {
            Directory.CreateDirectory(DeskVersePaths.LogsDirectory);
            var path = Path.Combine(DeskVersePaths.LogsDirectory, $"{DateTime.Now:yyyy-MM-dd}.log");
            File.AppendAllText(path, $"[{DateTimeOffset.Now:O}] {message}{Environment.NewLine}");
        }
        catch
        {
            // Ignore logging failures.
        }
    }
}
