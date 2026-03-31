using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using SignalForge.Application;

namespace SignalForge.Api.Authentication;

public sealed class HttpContextCurrentUser(IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
    public string? UserId
    {
        get
        {
            var user = httpContextAccessor.HttpContext?.User;
            if (user?.Identity?.IsAuthenticated != true)
                return null;

            return user.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? user.FindFirstValue(JwtRegisteredClaimNames.Sub)
                ?? user.FindFirstValue("sub");
        }
    }
}
