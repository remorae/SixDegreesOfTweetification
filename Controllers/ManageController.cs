using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using SixDegrees.Model;

namespace SixDegrees.Controllers
{
    [Route("manage/[action]")]
    public class ManageController : Controller
    {
        private readonly IConfiguration configuration;
        private readonly UserManager<ApplicationUser> userManager;
        private readonly SignInManager<ApplicationUser> signInManager;

        public ManageController(
               IConfiguration configuration,
               UserManager<ApplicationUser> userManager,
               SignInManager<ApplicationUser> signInManager)
        {
            this.configuration = configuration;
            this.userManager = userManager;
            this.signInManager = signInManager;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveExternal(string provider)
        {
            var user = await userManager.GetUserAsync(User);
            if (user == null)
            {
                throw new ApplicationException($"Unable to load user with ID '{userManager.GetUserId(User)}'.");
            }
            if (user.PasswordHash == null && (await userManager.GetLoginsAsync(user)).Count() < 2)
            {
                return BadRequest("No other authentication method for current user.");
            }

            var currentLogin = (await userManager.GetLoginsAsync(user)).Where(login => login.LoginProvider == provider).First();
            var result = await userManager.RemoveLoginAsync(user, currentLogin.LoginProvider, currentLogin.ProviderKey);
            if (!result.Succeeded)
            {
                throw new ApplicationException($"Unexpected error occurred removing external login for user with ID '{user.Id}'.");
            }

            await signInManager.SignInAsync(user, isPersistent: false);
            return Ok();
        }
        
        public async Task<IActionResult> LinkLogin(string provider)
        {
            // Clear the existing external cookie to ensure a clean login process
            await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);

            // Request a redirect to the external login provider to link a login for the current user
            var redirectUrl = Url.Action(nameof(LinkLoginCallback));
            var properties = signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl, userManager.GetUserId(User));
            return new ChallengeResult(provider, properties);
        }

        [HttpGet]
        public async Task<IActionResult> LinkLoginCallback()
        {
            var user = await userManager.GetUserAsync(User);
            if (user == null)
            {
                return BadRequest($"User is not authenticated.");
            }

            var info = await signInManager.GetExternalLoginInfoAsync(user.Id);
            if (info == null)
            {
                return BadRequest($"Could not load external login info.");
            }

            var result = await userManager.AddLoginAsync(user, info);
            if (!result.Succeeded)
            {
                return BadRequest($"Unexpected error occurred adding external login.");
            }

            // Clear the existing external cookie to ensure a clean login process
            await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);
            return AccountController.RedirectToLocal(this, Request, "/account");
        }
    }
}
