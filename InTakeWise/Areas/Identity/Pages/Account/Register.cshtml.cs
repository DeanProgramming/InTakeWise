using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Encodings.Web;
using InTakeWise.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;

namespace InTakeWise.Areas.Identity.Pages.Account;

[AllowAnonymous]
public class RegisterModel : PageModel
{
    private const int MaximumPasswordLength = 100;
    private const string DefaultReturnUrl = "/ProfileWizard/Step1";
    private const string EmailFieldName = "Input.Email";

    private readonly SignInManager<IdentityUser> _signInManager;
    private readonly UserManager<IdentityUser> _userManager;
    private readonly IEmailSender<IdentityUser> _emailSender;
    private readonly IConfiguration _configuration;
    private readonly ILogger<RegisterModel> _logger;

    public RegisterModel(
        SignInManager<IdentityUser> signInManager,
        UserManager<IdentityUser> userManager,
        IEmailSender<IdentityUser> emailSender,
        IConfiguration configuration,
        ILogger<RegisterModel> logger)
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _emailSender = emailSender;
        _configuration = configuration;
        _logger = logger;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public string ReturnUrl { get; private set; } = DefaultReturnUrl;

    public bool DemoEnabled { get; private set; }

    public IReadOnlyList<string> PasswordRequirements
    {
        get
        {
            var options = _userManager.Options.Password;

            var requirements = new List<string>
            {
                $"Between {options.RequiredLength} and {MaximumPasswordLength} characters"
            };

            if (options.RequireUppercase)
            {
                requirements.Add("At least one uppercase letter");
            }

            if (options.RequireLowercase)
            {
                requirements.Add("At least one lowercase letter");
            }

            if (options.RequireDigit)
            {
                requirements.Add("At least one number");
            }

            if (options.RequireNonAlphanumeric)
            {
                requirements.Add("At least one symbol");
            }

            if (options.RequiredUniqueChars > 1)
            {
                requirements.Add($"At least {options.RequiredUniqueChars} different characters");
            }

            return requirements;
        }
    }

    public sealed class InputModel
    {
        [Required]
        [EmailAddress]
        [StringLength(256)]
        [Display(Name = "Email")]
        public string Email { get; set; } = "";

        [Required]
        [StringLength(MaximumPasswordLength, ErrorMessage = "The password must be between {2} and {1} characters long.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; } = "";

        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "Confirm password")]
        [Compare(nameof(Password), ErrorMessage = "The password and confirmation password do not match.")]
        public string ConfirmPassword { get; set; } = "";
    }

    public void OnGet(string? returnUrl = null)
    {
        LoadPageState(returnUrl);
    }

    public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
    {
        LoadPageState(returnUrl);

        if (!ModelState.IsValid)
        {
            return Page();
        }

        var email = Input.Email.Trim();
        Input.Email = email;

        // This address is permanently reserved for the passwordless demo
        // account. Never allow a normal account to claim it.
        if (string.Equals(email, DbSeeder.DemoEmail, StringComparison.OrdinalIgnoreCase))
        {
            ModelState.AddModelError(EmailFieldName, "That email address is unavailable.");

            return Page();
        }

        // Create only the Identity account. UsersInformation is deliberately
        // created later when the user completes the profile wizard.
        var user = new IdentityUser
        {
            UserName = email,
            Email = email
        };

        var result = await _userManager.CreateAsync(user, Input.Password);

        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return Page();
        }

        // Do not add the demo claim, roles, profile information or other
        // application data here. A normal account begins with no claims.
        _logger.LogInformation("A new standard user account was created.");

        var userId = await _userManager.GetUserIdAsync(user);

        var confirmationToken = await _userManager.GenerateEmailConfirmationTokenAsync(user);

        var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(confirmationToken));

        var callbackUrl = Url.Page("/Account/ConfirmEmail", pageHandler: null,
            values: new
            {
                area = "Identity",
                userId,
                code = encodedToken,
                returnUrl = ReturnUrl
            },
            protocol: Request.Scheme);

        if (string.IsNullOrWhiteSpace(callbackUrl))
        {
            throw new InvalidOperationException("The email-confirmation URL could not be generated.");
        }

        await _emailSender.SendConfirmationLinkAsync(user, email, HtmlEncoder.Default.Encode(callbackUrl));

        if (_userManager.Options.SignIn.RequireConfirmedAccount)
        {
            return RedirectToPage(
                "./RegisterConfirmation",
                new
                {
                    email,
                    returnUrl = ReturnUrl
                });
        }

        await _signInManager.SignInAsync(user, isPersistent: false);

        return LocalRedirect(ReturnUrl);
    }

    private void LoadPageState(string? returnUrl)
    {
        ReturnUrl = GetSafeReturnUrl(returnUrl);

        DemoEnabled = _configuration.GetValue<bool>("Demo:Enabled");
    }

    private string GetSafeReturnUrl(string? returnUrl)
    {
        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return returnUrl;
        }

        return Url.Content($"~{DefaultReturnUrl}");
    }
}