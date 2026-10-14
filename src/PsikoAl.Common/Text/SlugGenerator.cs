using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace PsikoAl.Common.Text;

public static class SlugGenerator
{
    public const int MaxLength = 80;

    public const string Pattern = "^[a-z0-9]+(-[a-z0-9]+)*$";

    private static readonly Regex SlugRegex = new(Pattern, RegexOptions.Compiled, TimeSpan.FromMilliseconds(100));

    private static readonly CultureInfo Turkish = CultureInfo.GetCultureInfo("tr-TR");

    private static readonly Dictionary<char, string> TurkishFolds = new()
    {
        ['ç'] = "c",
        ['ğ'] = "g",
        ['ı'] = "i",
        ['ö'] = "o",
        ['ş'] = "s",
        ['ü'] = "u",
        ['â'] = "a",
        ['î'] = "i",
        ['û'] = "u",
    };

    public static string FromTitle(string? title)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            return string.Empty;
        }

        var lowered = title.Trim().ToLower(Turkish);
        var builder = new StringBuilder(lowered.Length);
        var pendingSeparator = false;

        foreach (var character in lowered)
        {
            var folded = Fold(character);
            if (folded is null)
            {
                pendingSeparator = builder.Length > 0;
                continue;
            }

            if (pendingSeparator)
            {
                builder.Append('-');
                pendingSeparator = false;
            }

            builder.Append(folded);
        }

        return Truncate(builder.ToString());
    }

    public static bool IsValid(string? slug)
        => !string.IsNullOrEmpty(slug)
            && slug.Length <= MaxLength
            && SlugRegex.IsMatch(slug);

    private static string? Fold(char character)
    {
        if (TurkishFolds.TryGetValue(character, out var replacement))
        {
            return replacement;
        }

        if (character is >= 'a' and <= 'z' or >= '0' and <= '9')
        {
            return character.ToString();
        }

        var decomposed = character.ToString().Normalize(NormalizationForm.FormD);
        var baseLetter = decomposed[0];
        return baseLetter is >= 'a' and <= 'z' ? baseLetter.ToString() : null;
    }

    private static string Truncate(string slug)
        => slug.Length <= MaxLength ? slug : slug[..MaxLength].TrimEnd('-');
}
