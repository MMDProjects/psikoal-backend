using System.Globalization;

namespace PsikoAl.Common.Text;

public static class BlogTagNormalizer
{
    public const int MaxTagCount = 10;

    public const int MaxTagLength = 40;

    private static readonly CultureInfo Turkish = CultureInfo.GetCultureInfo("tr-TR");

    public static IReadOnlyList<string> Normalize(IEnumerable<string>? tags)
        => (tags ?? [])
            .Select(NormalizeOne)
            .Where(tag => tag.Length > 0)
            .Distinct(StringComparer.Ordinal)
            .Take(MaxTagCount)
            .ToList();

    public static string NormalizeOne(string? tag)
    {
        if (string.IsNullOrWhiteSpace(tag))
        {
            return string.Empty;
        }

        var compact = string.Concat(tag.Trim().TrimStart('#').Where(character => !char.IsWhiteSpace(character)));
        var lowered = compact.ToLower(Turkish);
        return lowered.Length <= MaxTagLength ? lowered : lowered[..MaxTagLength];
    }
}
