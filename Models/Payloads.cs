namespace DeskVerse;

internal sealed record HitokotoPayload(
    [property: JsonPropertyName("hitokoto")] string? Hitokoto,
    [property: JsonPropertyName("from")] string? From,
    [property: JsonPropertyName("from_who")] string? FromWho,
    [property: JsonPropertyName("uuid")] string? Uuid);

internal sealed record JinrishiciTokenPayload(
    [property: JsonPropertyName("status")] string? Status,
    [property: JsonPropertyName("data")] string? Data);

internal sealed record JinrishiciPayload(
    [property: JsonPropertyName("status")] string? Status,
    [property: JsonPropertyName("data")] JinrishiciData? Data,
    [property: JsonPropertyName("errMessage")] string? ErrMessage);

internal sealed record JinrishiciData(
    [property: JsonPropertyName("content")] string? Content,
    [property: JsonPropertyName("origin")] JinrishiciOrigin? Origin);

internal sealed record JinrishiciOrigin(
    [property: JsonPropertyName("title")] string? Title,
    [property: JsonPropertyName("dynasty")] string? Dynasty,
    [property: JsonPropertyName("author")] string? Author);
