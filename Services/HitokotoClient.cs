namespace DeskVerse;

internal static class HitokotoClient
{
    private static readonly Uri ApiBaseUri = new("https://v1.hitokoto.cn/");

    public static async Task<HitokotoSentence> GetSentenceAsync()
    {
        var url = BuildApiUrl();
        using var response = await SentenceHttpClient.Shared.GetAsync(url);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync();
        var payload = await JsonSerializer.DeserializeAsync<HitokotoPayload>(stream, JsonOptions.Shared);

        if (payload?.Hitokoto is null)
        {
            throw new InvalidOperationException("Hitokoto API returned an empty sentence.");
        }

        return new HitokotoSentence(
            payload.Hitokoto,
            payload.From ?? "",
            payload.FromWho ?? "",
            payload.Uuid is null ? "https://hitokoto.cn" : $"https://hitokoto.cn?uuid={payload.Uuid}",
            SentenceSource.Hitokoto
        );
    }

    private static Uri BuildApiUrl()
    {
        var builder = new UriBuilder(ApiBaseUri);
        var parameters = new List<string>
        {
            "encode=json",
            "max_length=42"
        };

        foreach (var category in new[] { "a", "d", "e", "f", "i", "k" })
        {
            parameters.Add($"c={category}");
        }

        builder.Query = string.Join("&", parameters);
        return builder.Uri;
    }
}
