#nullable disable

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using InTakeWise.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace InTakeWise.Areas.Identity.Pages.Account
{
    [AllowAnonymous]
    public class LoginModel : PageModel
    {
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IConfiguration _configuration;
        private readonly ILogger<LoginModel> _logger;

        public LoginModel(
            SignInManager<IdentityUser> signInManager,
            UserManager<IdentityUser> userManager,
            IConfiguration configuration,
            ILogger<LoginModel> logger)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _configuration = configuration;
            _logger = logger;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public IList<AuthenticationScheme> ExternalLogins { get; set; }

        public string ReturnUrl { get; set; }

        public bool DemoEnabled { get; private set; }

        [TempData]
        public string ErrorMessage { get; set; }

        public class InputModel
        {
            [Required]
            [EmailAddress]
            public string Email { get; set; }

            [Required]
            [DataType(DataType.Password)]
            public string Password { get; set; }

            [Display(Name = "Remember me?")]
            public bool RememberMe { get; set; }
        }

        public async Task OnGetAsync(string returnUrl = null)
        {
            if (!string.IsNullOrEmpty(ErrorMessage))
            {
                ModelState.AddModelError(string.Empty, ErrorMessage);
            }

            // Clear any incomplete external-login cookie.
            await HttpContext.SignOutAsync(
                IdentityConstants.ExternalScheme);

            await LoadPageStateAsync(returnUrl);
        }

        public async Task<IActionResult> OnPostAsync(
            string returnUrl = null)
        {
            await LoadPageStateAsync(returnUrl);

            if (!ModelState.IsValid)
            {
                return Page();
            }

            var result = await _signInManager.PasswordSignInAsync(
                Input.Email,
                Input.Password,
                Input.RememberMe,
                lockoutOnFailure: true);

            if (result.Succeeded)
            {
                _logger.LogInformation("User logged in.");
                return LocalRedirect(ReturnUrl);
            }

            if (result.RequiresTwoFactor)
            {
                return RedirectToPage(
                    "./LoginWith2fa",
                    new
                    {
                        ReturnUrl,
                        RememberMe = Input.RememberMe
                    });
            }

            if (result.IsLockedOut)
            {
                _logger.LogWarning("User account locked out.");
                return RedirectToPage("./Lockout");
            }

            ModelState.AddModelError(
                string.Empty,
                "Invalid login attempt.");

            return Page();
        }

        public async Task<IActionResult> OnPostDemoAsync(
            string returnUrl = null)
        {
            var safeReturnUrl = GetSafeReturnUrl(returnUrl);

            if (!_configuration.GetValue<bool>("Demo:Enabled"))
            {
                _logger.LogWarning("A demo sign-in was attempted while demo mode was disabled.");

                return NotFound();
            }

            var demoUser = await _userManager.FindByEmailAsync(
                DbSeeder.DemoEmail);

            if (demoUser is null)
            {
                _logger.LogError("Demo sign-in failed because the seeded demo user was not found.");

                return await DemoUnavailableAsync(safeReturnUrl);
            }

            var claims = await _userManager.GetClaimsAsync(demoUser);

            var hasDemoClaim = claims.Any(claim =>
                claim.Type == DbSeeder.DemoClaimType &&
                claim.Value == DbSeeder.DemoClaimValue);

            var hasPassword =
                await _userManager.HasPasswordAsync(demoUser);

            // Do not sign in solely because the email matches.
            // These checks prove step 1 completed safely.
            if (!hasDemoClaim ||
                hasPassword ||
                !demoUser.EmailConfirmed)
            {
                _logger.LogCritical(
                    "Refused demo sign-in because the account was not safely configured. " +
                    "HasDemoClaim: {HasDemoClaim}; " +
                    "IsPasswordless: {IsPasswordless}; " +
                    "EmailConfirmed: {EmailConfirmed}.",
                    hasDemoClaim,
                    !hasPassword,
                    demoUser.EmailConfirmed);

                return await DemoUnavailableAsync(safeReturnUrl);
            }

            try
            {
                await DemoDataSeeder.SeedAsync(HttpContext.RequestServices, HttpContext.RequestAborted);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Demo sample-data refresh failed.");

                return await DemoUnavailableAsync(safeReturnUrl);
            }

            await _signInManager.SignInAsync(
                demoUser,
                isPersistent: false);

            _logger.LogInformation("Demo user signed in.");

            return LocalRedirect(safeReturnUrl);
        }

        private async Task<IActionResult> DemoUnavailableAsync(
            string safeReturnUrl)
        {
            await LoadPageStateAsync(safeReturnUrl);

            ModelState.AddModelError(string.Empty, "The demo is temporarily unavailable. Please try again later.");

            return Page();
        }

        private async Task LoadPageStateAsync(string returnUrl)
        {
            ReturnUrl = GetSafeReturnUrl(returnUrl);

            DemoEnabled = _configuration.GetValue<bool>("Demo:Enabled");

            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();
        }

        private string GetSafeReturnUrl(string returnUrl)
        {
            if (!string.IsNullOrWhiteSpace(returnUrl) &&
                Url.IsLocalUrl(returnUrl))
            {
                return returnUrl;
            }

            return Url.Content("~/");
        }
    }
}