using System.Security.Claims;
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
            role = User.FindFirstValue("user_role"),
            claims = User.Claims.Select(claim => new { claim.Type, claim.Value }),
        });
}
