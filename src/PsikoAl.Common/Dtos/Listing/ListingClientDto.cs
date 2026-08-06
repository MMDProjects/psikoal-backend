namespace PsikoAl.Common.Dtos.Listing;

// Eşleşme öncesi uzmana danışanın tam adı hiç gönderilmez — bilinçli olarak
// yalnızca maskelenmiş görünen ad + initials + avatar taşınır (bkz. ListingDto.ClientDisplayName).
public sealed record ListingClientDto(
    Guid Id,
    string Initials,
    string? AvatarUrl,
    DateTimeOffset CreatedAt);
