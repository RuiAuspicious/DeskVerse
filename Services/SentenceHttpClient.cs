namespace DeskVerse;

internal static class SentenceHttpClient
{
    private static readonly TimeSpan RequestTimeout = TimeSpan.FromSeconds(8);

    public static HttpClient Shared { get; } = Create();

    private static HttpClient Create()
    {
        var client = new HttpClient
        {
            Timeout = RequestTimeout
        };
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        client.DefaultRequestHeaders.UserAgent.ParseAdd("deskverse/0.1");
        return client;
    }
}
