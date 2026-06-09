namespace DeskVerse;

internal static class JinrishiciClient
{
    private static readonly Uri TokenUri = new("https://v2.jinrishici.com/token");
    private static readonly Uri SentenceUri = new("https://v2.jinrishici.com/sentence");

    public static async Task<HitokotoSentence> GetSentenceAsync()
    {
        var hadCachedToken = !string.IsNullOrWhiteSpace(AppSettings.Load().JinrishiciToken);
        var token = await GetTokenAsync();
        try
        {
            return await RequestSentenceAsync(token);
        }
        catch (Exception exception) when (hadCachedToken)
        {
            AppLogger.Log(exception, "Jinrishici request failed with cached token; clearing token and retrying once");
            var settings = AppSettings.Load();
            AppSettings.Save(settings with { JinrishiciToken = null });
            return await RequestSentenceAsync(await GetTokenAsync());
        }
    }

    private static async Task<HitokotoSentence> RequestSentenceAsync(string token)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, SentenceUri);
        request.Headers.Add("X-User-Token", token);

        using var response = await SentenceHttpClient.Shared.SendAsync(request);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync();
        var payload = await JsonSerializer.DeserializeAsync<JinrishiciPayload>(stream, JsonOptions.Shared);

        if (payload?.Status != "success" || payload.Data?.Content is null)
        {
            throw new InvalidOperationException(payload?.ErrMessage ?? "Jinrishici API returned an empty sentence.");
        }

        var origin = payload.Data.Origin;
        var from = origin?.Title ?? "";
        var fromWho = string.Join(" · ", new[] { origin?.Dynasty, origin?.Author }.Where(value => !string.IsNullOrWhiteSpace(value)));

        return new HitokotoSentence(
            payload.Data.Content,
            from,
            fromWho,
            "https://www.jinrishici.com/",
            SentenceSource.Jinrishici
        );
    }

    private static async Task<string> GetTokenAsync()
    {
        var settings = AppSettings.Load();
        if (!string.IsNullOrWhiteSpace(settings.JinrishiciToken))
        {
            return settings.JinrishiciToken;
        }

        using var response = await SentenceHttpClient.Shared.GetAsync(TokenUri);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync();
        var payload = await JsonSerializer.DeserializeAsync<JinrishiciTokenPayload>(stream, JsonOptions.Shared);
        if (payload?.Status != "success" || string.IsNullOrWhiteSpace(payload.Data))
        {
            throw new InvalidOperationException("Jinrishici API did not return a token.");
        }

        AppSettings.Save(settings with { JinrishiciToken = payload.Data });
        return payload.Data;
    }
}
