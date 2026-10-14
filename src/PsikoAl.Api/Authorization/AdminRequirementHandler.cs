using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using PsikoAl.Common.Constants;
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

        var cancellationToken = (context.Resource as HttpContext)?.RequestAborted ?? CancellationToken.None;
        var admin = await adminGuard.GetActiveAdminAsync(authUserId, cancellationToken);
        if (admin is not null && AdminRoleGroups.Allows(requirement.AllowedRoles, admin.Role))
        {
            context.Succeed(requirement);
        }
    }
}
