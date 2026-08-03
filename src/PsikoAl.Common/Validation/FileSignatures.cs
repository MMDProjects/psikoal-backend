namespace PsikoAl.Common.Validation;

public static class FileSignatures
{
    private static readonly Dictionary<string, byte[][]> SignaturesByContentType = new()
    {
        ["image/jpeg"] = [[0xFF, 0xD8, 0xFF]],
        ["image/png"] = [[0x89, 0x50, 0x4E, 0x47]],
        ["image/webp"] = [[0x52, 0x49, 0x46, 0x46]],
        ["application/pdf"] = [[0x25, 0x50, 0x44, 0x46]],
    };

    public static bool IsSupported(string contentType) => SignaturesByContentType.ContainsKey(contentType);

    public static bool Matches(string contentType, ReadOnlySpan<byte> header)
    {
        if (!SignaturesByContentType.TryGetValue(contentType, out var signatures))
        {
            return false;
        }

        foreach (var signature in signatures)
        {
            if (header.Length >= signature.Length && header[..signature.Length].SequenceEqual(signature))
            {
                return true;
            }
        }

        return false;
    }

    public static string ExtensionFor(string contentType) => contentType switch
    {
        "image/jpeg" => "jpg",
        "image/png" => "png",
        "image/webp" => "webp",
        "application/pdf" => "pdf",
        _ => "bin",
    };
}
