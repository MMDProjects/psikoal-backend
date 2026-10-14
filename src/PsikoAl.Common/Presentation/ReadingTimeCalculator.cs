namespace PsikoAl.Common.Presentation;

public static class ReadingTimeCalculator
{
    public const int WordsPerMinute = 200;

    public static int MinutesFor(string? content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return 1;
        }

        var wordCount = content.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Length;
        return Math.Max(1, (int)Math.Ceiling(wordCount / (double)WordsPerMinute));
    }
}
