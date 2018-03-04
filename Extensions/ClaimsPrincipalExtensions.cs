using System.Linq;
using System.Security.Claims;

namespace SixDegrees.Extensions
{
    static class ClaimsPrincipalExtensions
    {
        internal static string GetTwitterAccessToken(this ClaimsPrincipal user)
            => (user.Identity as ClaimsIdentity)?.Claims.FirstOrDefault(claim => claim.Type == Startup.AccessTokenClaim)?.Value;
        internal static string GetTwitterAccessTokenSecret(this ClaimsPrincipal user)
            => (user.Identity as ClaimsIdentity)?.Claims.FirstOrDefault(claim => claim.Type == Startup.AccessTokenSecretClaim)?.Value;
    }
}
