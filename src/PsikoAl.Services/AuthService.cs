using PsikoAl.Common.Constants;
using PsikoAl.Common.Dtos.Auth;
using PsikoAl.Common.Dtos.Auth.Create;
using PsikoAl.Common.Exceptions;
using PsikoAl.Data.Entities;
using PsikoAl.Data.Repositories.Abstractions;
using PsikoAl.Services.Abstractions;
using PsikoAl.Services.Mapping;

namespace PsikoAl.Services;

public sealed class AuthService(
    ISupabaseAuthService supabaseAuth,
    ISupabaseAdminService supabaseAdmin,
    IUnitOfWork unitOfWork) : IAuthService
{
    public async Task<LoginResponseDto> LoginAsync(LoginRequestDto request, CancellationToken cancellationToken)
    {
        var session = await supabaseAuth.LoginAsync(request, cancellationToken);
        var profile = await GetActiveProfileAsync(session.UserId, cancellationToken);
        return new LoginResponseDto(ProfileMapper.ToAuthUserDto(profile), session.Tokens);
    }

    public async Task<LoginResponseDto> RegisterAsync(RegisterRequestDto request, CancellationToken cancellationToken)
    {
        // Signup yerine admin-create (email_confirm) + login: e-posta doğrulama beklemeden
        // frontend'in beklediği session'ı döndürür ve signup e-posta rate limitine takılmaz.
        await supabaseAdmin.CreateConfirmedUserAsync(
            request.Email,
            request.Password,
            request.FirstName,
            request.LastName,
            request.Role,
            cancellationToken);

        return await LoginAsync(new LoginRequestDto(request.Email, request.Password), cancellationToken);
    }

    public async Task ChangePasswordAsync(
        Guid userId,
        string email,
        ChangePasswordRequestDto request,
        CancellationToken cancellationToken)
    {
        if (request.NewPassword != request.ConfirmPassword)
        {
            throw new DomainException(ErrorKeys.ValidationFailed, "confirmPassword");
        }

        await supabaseAuth.LoginAsync(new LoginRequestDto(email, request.CurrentPassword), cancellationToken);
        await supabaseAdmin.SetPasswordAsync(userId, request.NewPassword, cancellationToken);
    }

    public async Task ForgotPasswordAsync(ForgotPasswordRequestDto request, CancellationToken cancellationToken)
    {
        // Hata durumunda da sessiz kalınır: yanıt üzerinden kayıtlı e-posta taraması yapılamamalı.
        try
        {
            await supabaseAuth.SendPasswordRecoveryAsync(request.Email, cancellationToken);
        }
        catch (HttpRequestException)
        {
        }
    }

    private async Task<Profile> GetActiveProfileAsync(Guid userId, CancellationToken cancellationToken)
    {
        var profile = await unitOfWork.Profiles.GetByIdAsync(userId, cancellationToken)
            ?? throw new DomainException(ErrorKeys.ProfileNotFound);

        if (profile.Status != ProfileStatuses.Active)
        {
            throw new DomainException(ErrorKeys.AuthAccountFrozen);
        }

        return profile;
    }
}
