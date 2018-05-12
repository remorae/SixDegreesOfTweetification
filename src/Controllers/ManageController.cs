using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using SixDegrees.Model;

namespace SixDegrees.Controllers
{
    /// <summary>
    /// Handles account management for authenticated users.
    /// </summary>
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

        /// <summary>
        /// Removes the given external login provider from the current user.
        /// </summary>
        /// <param name="provider"></param>
        /// <returns></returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveExternal(string provider)
        {
            if (provider == null)
                return BadRequest("Invalid parameters.");
            var user = await userManager.GetUserAsync(User);
            if (user == null)
            {
                return BadRequest($"Unable to load user with ID '{userManager.GetUserId(User)}'.");
            }
            if (user.PasswordHash == null && (await userManager.GetLoginsAsync(user)).Count() < 2)
            {
                return BadRequest("No other authentication method for current user.");
            }

            var currentLogin = (await userManager.GetLoginsAsync(user)).Where(login => login.LoginProvider == provider).FirstOrDefault();
            var result = await userManager.RemoveLoginAsync(user, currentLogin.LoginProvider, currentLogin.ProviderKey);
            if (!result.Succeeded)
            {
                return BadRequest($"Unexpected error occurred removing external login for user with ID '{user.Id}'.");
            }

            await signInManager.SignInAsync(user, isPersistent: false);
            return Ok();
        }
        
        /// <summary>
        /// Begins the external login process to add a new external login provider to the current user.
        /// </summary>
        /// <param name="provider"></param>
        /// <returns></returns>
        public async Task<IActionResult> LinkLogin(string provider)
        {
            if (provider == null)
                return BadRequest("Invalid parameters.");
            await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);

            var redirectUrl = Url.Action(nameof(LinkLoginCallback));
            var properties = signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl, userManager.GetUserId(User));
            return new ChallengeResult(provider, properties);
        }

        /// <summary>
        /// Called from external login providers upon successful authentication; connects the external login method to the current user.
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public async Task<IActionResult> LinkLoginCallback()
        {
            var user = await userManager.GetUserAsync(User);
            if (user == null)
            {
                return Unauthorized();
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
            
            await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);
            return AccountController.RedirectToLocal(this, Request, "/account");
        }
    }
}
