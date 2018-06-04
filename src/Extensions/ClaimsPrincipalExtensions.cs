using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using SixDegrees.Model;

namespace SixDegrees.Extensions
{
    static class ClaimsPrincipalExtensions
    {
        internal static string GetTwitterAccessToken(this ClaimsPrincipal user)
            => (user.Identity as ClaimsIdentity)?.Claims.FirstOrDefault(claim => claim.Type == Startup.AccessTokenClaim)?.Value;
        internal static string GetTwitterAccessTokenSecret(this ClaimsPrincipal user)
            => (user.Identity as ClaimsIdentity)?.Claims.FirstOrDefault(claim => claim.Type == Startup.AccessTokenSecretClaim)?.Value;

        internal static async Task<ApplicationUser> GetCurrentApplicationUser(this UserManager<ApplicationUser> manager, ClaimsPrincipal user)
        {
            return (user.Identity.Name != null) ? await manager.FindByNameAsync(user.Identity.Name) : null;
        }
    }
}
