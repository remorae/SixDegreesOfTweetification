using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SixDegrees.Extensions;
using SixDegrees.Model;
using SixDegrees.Model.AccountViewModel;
using SixDegrees.Services;

namespace SixDegrees.Controllers
{
    [Route("account/[action]")]
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> userManager;
        private readonly SignInManager<ApplicationUser> signInManager;
        private readonly IEmailSender emailSender;
        private readonly ILogger logger;
        private readonly IConfiguration configuration;

        public AccountController(
            IConfiguration configuration,
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IEmailSender emailSender,
            ILogger<AccountController> logger)
        {
            this.configuration = configuration;
            this.userManager = userManager;
            this.signInManager = signInManager;
            this.emailSender = emailSender;
            this.logger = logger;
        }

        [TempData]
        public string ErrorMessage { get; set; }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> Login(string returnUrl = null)
        {
            await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);

            return Ok();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login([FromBody] LoginModel model, string returnUrl = null)
        {
            if (TryValidateModel(model))
            {
                var result = await signInManager.PasswordSignInAsync(model.Email, model.Password, model.RememberMe, lockoutOnFailure: false);
                if (result.Succeeded)
                {
                    logger.LogInformation("User logged in.");
                    return Ok();
                }
                else if (result.IsLockedOut)
                {
                    logger.LogWarning("User account locked out.");
                    return RedirectToAction(nameof(Lockout));
                }
                else
                {
                    return BadRequest("Invalid login attempt.");
                }
            }
            return BadRequest("Illegal credentials.");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await signInManager.SignOutAsync();
            logger.LogInformation("User logged out.");
            return Ok();
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Lockout()
        {
            return BadRequest("Account locked out.");
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register([FromBody] RegistrationModel model, string returnUrl = null)
        {
            if (TryValidateModel(model))
            {
                var user = new ApplicationUser { UserName = model.Email, Email = model.Email };
                var result = await userManager.CreateAsync(user, model.Password);
                if (result.Succeeded)
                {
                    logger.LogInformation("User created a new account with password.");

                    var code = await userManager.GenerateEmailConfirmationTokenAsync(user);
                    var callbackUrl = Url.EmailConfirmationLink(user.Id, code, Request.Scheme);
                    await emailSender.SendEmailConfirmationAsync(model.Email, callbackUrl);

                    await signInManager.SignInAsync(user, isPersistent: false);
                    logger.LogInformation("User created a new account with password.");
                    return Ok(returnUrl);
                }

                return BadRequest($"Error during registration:\n{string.Join("\n", result.Errors.Select(error => $"{error.Code}: {error.Description}"))}");
            }

            return BadRequest($"Invalid registration info: {string.Join(";", ModelState.Values.SelectMany(v => v.Errors.Select(b => b.ErrorMessage)))}");
        }

        [AllowAnonymous]
        public async Task<IActionResult> ExternalLogin(string provider, string returnUrl = null)
        {
            if (provider == null)
                return BadRequest("Invalid parameters.");
            await signInManager.SignOutAsync();
            var redirectUrl = Url.Action(nameof(ExternalLoginCallback), "Account", new { returnUrl });
            var properties = signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);
            return Challenge(properties, provider);
        }

        [HttpGet]
        public async Task<IActionResult> ExternalLoginCallback(string returnUrl = null, string remoteError = null)
        {
            if (remoteError != null)
            {
                return RedirectToLocal(this, Request, "/login");
            }
            var info = await signInManager.GetExternalLoginInfoAsync();
            if (info == null)
            {
                // No token to assign
                return RedirectToLocal(this, Request, "/login");
            }

            // Sign in the user with this external login provider if the user already has a login.
            var result = await signInManager.ExternalLoginSignInAsync(info.LoginProvider, info.ProviderKey, isPersistent: false, bypassTwoFactor: true);
            if (result.Succeeded)
            {
                return RedirectToLocal(this, Request, returnUrl);
            }
            if (result.IsLockedOut)
            {
                return RedirectToAction(nameof(Lockout));
            }
            else
            {
                // If the user does not have an account, then ask the user to create an account.
                return RedirectToLocal(this, Request, "/externallogin");
            }
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ExternalLoginConfirmation([FromBody] ExternalLoginModel model, string returnUrl = null)
        {
            if (TryValidateModel(model))
            {
                var info = await signInManager.GetExternalLoginInfoAsync();
                if (info == null)
                {
                    throw new ApplicationException("Error loading external login information during confirmation.");
                }
                var user = new ApplicationUser { UserName = model.Email, Email = model.Email };
                var result = await userManager.CreateAsync(user, model.Password);
                if (result.Succeeded)
                {
                    result = await userManager.AddLoginAsync(user, info);
                    if (result.Succeeded)
                    {
                        result = await userManager.AddClaimsAsync(user, info.Principal.Claims);
                        if (result.Succeeded)
                        {
                            await signInManager.SignInAsync(user, isPersistent: false);
                            return Ok(returnUrl);
                        }
                    }
                }

                return BadRequest($"Error during registration:\n{string.Join("\n", result.Errors.Select(error => $"{error.Code}: {error.Description}"))}");
            }

            return BadRequest("Not a valid email.");
        }

        internal static IActionResult RedirectToLocal(ControllerBase controller, HttpRequest request, string localUrl)
        {
            if (controller == null || request == null)
                throw new ArgumentNullException("Null controller or request.");
            if (localUrl == null)
                localUrl = "/home";
            else
            {
                Regex regex = new Regex(@"^/[^\s]*");
                if (!regex.IsMatch(localUrl))
                    localUrl = "/home";
            }
            return controller.Redirect($"https://{request.Host.Value}{localUrl}");
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> ConfirmEmail(string userId, string code)
        {
            if (userId == null || code == null)
            {
                return Ok();
            }
            var user = await userManager.FindByIdAsync(userId);
            if (user == null)
            {
                throw new ApplicationException($"Unable to load user with ID '{userId}'.");
            }
            var result = await userManager.ConfirmEmailAsync(user, code);
            if (result.Succeeded)
                return Ok();
            else
                return BadRequest("Error");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<bool> TwitterAvailable()
        {
            if (!User.Identity.IsAuthenticated)
                return false;
            var logins = await userManager.GetLoginsAsync(await userManager.GetUserAsync(User));
            return logins.Where(login => login.LoginProvider == "Twitter").Count() > 0;
        }

        [HttpPost]
        public bool Authenticated()
        {
            return User.Identity.IsAuthenticated;
        }
    }
}