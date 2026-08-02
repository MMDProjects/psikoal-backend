using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace PsikoAl.Api.Controllers;

[ApiController]
[Route("auth")]
public sealed class MeController : ControllerBase
{
    [Authorize]
    [HttpGet("whoami")]
    public IActionResult WhoAmI()
        => Ok(new
        {
            userId = User.FindFirstValue(ClaimTypes.NameIdentifier),
            email = User.FindFirstValue(ClaimTypes.Email),
            role = ReadRoleFromUserMetadata(User),
            claims = User.Claims.Select(claim => new { claim.Type, claim.Value }),
        });

    private static string? ReadRoleFromUserMetadata(ClaimsPrincipal user)
    {
        var userMetadataJson = user.FindFirstValue("user_metadata");
        if (userMetadataJson is null)
        {
            return null;
        }

        using var document = JsonDocument.Parse(userMetadataJson);
        return document.RootElement.TryGetProperty("role", out var role) ? role.GetString() : null;
    }
}
