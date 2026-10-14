using Microsoft.AspNetCore.Authorization;

namespace PsikoAl.Api.Authorization;

public sealed class AdminRequirement(IReadOnlyList<string> allowedRoles) : IAuthorizationRequirement
{
    public IReadOnlyList<string> AllowedRoles { get; } = allowedRoles;
}
