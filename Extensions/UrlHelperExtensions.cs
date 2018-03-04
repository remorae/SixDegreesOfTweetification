using Microsoft.AspNetCore.Mvc;
using SixDegrees.Controllers;

namespace SixDegrees.Extensions
{
    internal static class UrlHelperExtensions
    {
        public static string EmailConfirmationLink(this IUrlHelper urlHelper, string userId, string code, string scheme)
        {
            return urlHelper.Action(
                action: nameof(AuthenticationController.ConfirmEmail),
                controller: "Authentication",
                values: new { userId, code },
                protocol: scheme);
        }
    }
}
