namespace DeskVerse;

internal sealed record HitokotoSentence(string Text, string From, string FromWho, string Link, SentenceSource Source);

internal sealed record FavoriteSentence(
    string Text,
    string From,
    string FromWho,
    string Link,
    SentenceSource Source,
    DateTimeOffset SavedAt)
{
    public static FavoriteSentence FromSentence(HitokotoSentence sentence)
    {
        return new FavoriteSentence(
            sentence.Text,
            sentence.From,
            sentence.FromWho,
            sentence.Link,
            sentence.Source,
            DateTimeOffset.Now);
    }
}
