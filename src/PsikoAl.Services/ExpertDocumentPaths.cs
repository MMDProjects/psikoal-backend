using System.Diagnostics.CodeAnalysis;

namespace PsikoAl.Services;

/// <summary>Uzman belgesinin Storage yolu yalnız o uzmanın klasöründe olabilir.</summary>
/// <remarks>
/// Admin belge ucu yolları service_role ile imzalıyor (RLS yok). certificates/cv_url bir
/// dönem istemciden serbest metin olarak kabul edildiği için, başka bir kullanıcının
/// klasörünü gösteren bir yol imzalatılıp o kişinin belgeleri açılabilirdi.
/// </remarks>
public static class ExpertDocumentPaths
{
    public const int MaxCertificates = 10;

    public static bool IsOwnedBy(Guid expertId, [NotNullWhen(true)] string? path)
        => !string.IsNullOrWhiteSpace(path)
            && path.StartsWith($"{expertId}/", StringComparison.OrdinalIgnoreCase)
            && path.Length > expertId.ToString().Length + 1
            && !path.Contains("..", StringComparison.Ordinal)
            && !path.Contains("//", StringComparison.Ordinal)
            && !path.Contains('\\', StringComparison.Ordinal)
            && !path.Contains('?', StringComparison.Ordinal)
            && !path.Contains('#', StringComparison.Ordinal);
}
