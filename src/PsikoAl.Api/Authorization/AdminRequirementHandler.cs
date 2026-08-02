using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using PsikoAl.Services.Abstractions;

namespace PsikoAl.Api.Authorization;

public sealed class AdminRequirementHandler(IAdminGuard adminGuard)
    : AuthorizationHandler<AdminRequirement>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        AdminRequirement requirement)
    {
        var value = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(value, out var authUserId))
        {
            return;
        }

        var admin = await adminGuard.GetActiveAdminAsync(authUserId, CancellationToken.None);
        if (admin is not null)
        {
            context.Succeed(requirement);
        }
    }
}
