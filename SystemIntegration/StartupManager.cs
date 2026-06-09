namespace DeskVerse;

internal static class StartupManager
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string AppName = "DeskVerse";

    public static bool IsEnabled()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, false);
        var value = key?.GetValue(AppName) as string;
        return string.Equals(ExtractExecutablePath(value), Application.ExecutablePath, StringComparison.OrdinalIgnoreCase);
    }

    public static void SetEnabled(bool enabled)
    {
        using var key = Registry.CurrentUser.CreateSubKey(RunKeyPath, true);
        if (key is null)
        {
            return;
        }

        if (enabled)
        {
            key.SetValue(AppName, BuildRunCommand(Application.ExecutablePath));
        }
        else
        {
            key.DeleteValue(AppName, false);
        }
    }

    internal static string BuildRunCommand(string executablePath)
    {
        return $"\"{executablePath}\"";
    }

    internal static string? ExtractExecutablePath(string? runCommand)
    {
        if (string.IsNullOrWhiteSpace(runCommand))
        {
            return null;
        }

        var trimmed = runCommand.Trim();
        if (!trimmed.StartsWith('"'))
        {
            return trimmed.Split(' ', 2)[0];
        }

        var closingQuote = trimmed.IndexOf('"', 1);
        return closingQuote <= 1 ? trimmed.Trim('"') : trimmed[1..closingQuote];
    }
}
