using System.Text.Json;
using PsikoAl.Common.Constants;
using PsikoAl.Common.Dtos.Auth;
using PsikoAl.Common.Dtos.Auth.Update;
using PsikoAl.Common.Exceptions;
using PsikoAl.Data.Entities;
using PsikoAl.Data.Repositories.Abstractions;
using PsikoAl.Services.Abstractions;
using PsikoAl.Services.Mapping;

namespace PsikoAl.Services;

public sealed class ProfileService(
    IUnitOfWork unitOfWork,
    ISupabaseAdminService supabaseAdmin) : IProfileService
{
    public async Task<AuthUserDto> GetMeAsync(Guid userId, CancellationToken cancellationToken)
    {
        var profile = await GetRequiredProfileAsync(userId, cancellationToken);
        return ProfileMapper.ToAuthUserDto(profile);
    }

    public async Task<AuthUserDto> UpdateMeAsync(Guid userId, UpdateProfileDto request, CancellationToken cancellationToken)
    {
        var profile = await GetRequiredProfileAsync(userId, cancellationToken);

        if (request.FirstName is not null)
        {
            profile.FirstName = request.FirstName;
        }

        if (request.LastName is not null)
        {
            profile.LastName = request.LastName;
        }

        if (request.Phone is not null)
        {
            profile.Phone = request.Phone.Length == 0 ? null : request.Phone;
        }

        if (request.City is not null)
        {
            profile.City = request.City.Length == 0 ? null : request.City;
        }

        if (request.ShareEmail.HasValue)
        {
            profile.ShareEmail = request.ShareEmail.Value;
        }

        if (request.SharePhone.HasValue)
        {
            profile.SharePhone = request.SharePhone.Value;
        }

        if (request.ShareLocation.HasValue)
        {
            profile.ShareLocation = request.ShareLocation.Value;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return ProfileMapper.ToAuthUserDto(profile);
    }

    public async Task FreezeMeAsync(Guid userId, CancellationToken cancellationToken)
    {
        var profile = await GetRequiredProfileAsync(userId, cancellationToken);
        var oldStatus = profile.Status;

        profile.Status = ProfileStatuses.Frozen;
        await supabaseAdmin.BanUserAsync(userId, cancellationToken);
        await AddStatusAuditAsync(profile.Id, "user.freeze_self", oldStatus, profile.Status, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteMeAsync(Guid userId, CancellationToken cancellationToken)
    {
        var profile = await GetRequiredProfileAsync(userId, cancellationToken);
        var oldStatus = profile.Status;

        // KVKK: kayıt silinmez, anonimleştirilir; auth tarafında kalıcı ban ile giriş kapatılır.
        profile.Email = $"deleted-{profile.Id}@psikoal.invalid";
        profile.FirstName = "Silinmiş";
        profile.LastName = "Kullanıcı";
        profile.Phone = null;
        profile.City = null;
        profile.AvatarUrl = null;
        profile.Status = ProfileStatuses.Deleted;

        await supabaseAdmin.BanUserAsync(userId, cancellationToken);
        await AddStatusAuditAsync(profile.Id, "user.delete_self", oldStatus, profile.Status, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task<Profile> GetRequiredProfileAsync(Guid userId, CancellationToken cancellationToken)
        => await unitOfWork.Profiles.GetByIdAsync(userId, cancellationToken)
            ?? throw new DomainException(ErrorKeys.ProfileNotFound);

    private async Task AddStatusAuditAsync(
        Guid profileId,
        string action,
        string oldStatus,
        string newStatus,
        CancellationToken cancellationToken)
        => await unitOfWork.AuditLogs.AddAsync(
            new AuditLog
            {
                ActorType = AuditActorTypes.User,
                Action = action,
                EntityType = "profile",
                EntityId = profileId.ToString(),
                OldValue = JsonSerializer.Serialize(new { status = oldStatus }),
                NewValue = JsonSerializer.Serialize(new { status = newStatus }),
            },
            cancellationToken);
}
