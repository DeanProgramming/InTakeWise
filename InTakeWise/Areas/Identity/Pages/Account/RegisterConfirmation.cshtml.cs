using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;

namespace InTakeWise.Areas.Identity.Pages.Account;

[AllowAnonymous]
public class RegisterConfirmationModel : PageModel
{
    private const string DefaultReturnUrl =
        "/ProfileWizard/Step1";

    private readonly UserManager<IdentityUser> _userManager;
    private readonly IConfiguration _configuration;

    public RegisterConfirmationModel(
        UserManager<IdentityUser> userManager,
        IConfiguration configuration)
    {
        _userManager = userManager;
        _configuration = configuration;
    }

    public string Email { get; private set; } = "";

    public string ReturnUrl { get; private set; } =
        DefaultReturnUrl;

    public bool DisplayConfirmAccountLink { get; private set; }

    public bool AccountAlreadyConfirmed { get; private set; }

    public string? EmailConfirmationUrl { get; private set; }

    public bool DemoEnabled { get; private set; }
     

    public async Task<IActionResult> OnGetAsync(
        string? email,
        string? returnUrl = null)
    {
        ReturnUrl = GetSafeReturnUrl(returnUrl);
        DemoEnabled = _configuration.GetValue<bool>("Demo:Enabled");

        if (string.IsNullOrWhiteSpace(email))
        {
            return RedirectToPage(
                "./Register",
                new { returnUrl = ReturnUrl });
        }

        Email = email.Trim();

        var user = await _userManager.FindByEmailAsync(Email);

        // Keep the response generic if somebody manually requests this
        // page with an email address that does not belong to an account.
        if (user is null)
        {
            return Page();
        }

        AccountAlreadyConfirmed =
            await _userManager.IsEmailConfirmedAsync(user);

        if (AccountAlreadyConfirmed)
        {
            return Page();
        }

        DisplayConfirmAccountLink =
            _configuration.GetValue<bool>(
                "Registration:ShowConfirmationLink");

        if (!DisplayConfirmAccountLink)
        {
            return Page();
        }

        var userId = await _userManager.GetUserIdAsync(user);

        var confirmationToken =
            await _userManager.GenerateEmailConfirmationTokenAsync(user);

        var encodedToken = WebEncoders.Base64UrlEncode(
            Encoding.UTF8.GetBytes(confirmationToken));

        EmailConfirmationUrl = Url.Page(
            "/Account/ConfirmEmail",
            pageHandler: null,
            values: new
            {
                area = "Identity",
                userId,
                code = encodedToken,
                returnUrl = ReturnUrl
            },
            protocol: Request.Scheme);

        return Page();
    }

    private string GetSafeReturnUrl(string? returnUrl)
    {
        if (!string.IsNullOrWhiteSpace(returnUrl) &&
            Url.IsLocalUrl(returnUrl))
        {
            return returnUrl;
        }

        return Url.Content($"~{DefaultReturnUrl}");
    }
}