namespace DeskVerse;

internal static class JsonOptions
{
    public static JsonSerializerOptions Shared { get; } = new()
    {
        PropertyNameCaseInsensitive = true
    };
}
