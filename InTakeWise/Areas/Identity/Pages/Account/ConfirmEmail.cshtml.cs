using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;

[AllowAnonymous]
public class ConfirmEmailModel : PageModel
{
    private const string DefaultReturnUrl = "/ProfileWizard/Step1";

    private readonly UserManager<IdentityUser> _userManager;
    private readonly SignInManager<IdentityUser> _signInManager;
    private readonly ILogger<ConfirmEmailModel> _logger;

    public ConfirmEmailModel(
        UserManager<IdentityUser> userManager,
        SignInManager<IdentityUser> signInManager,
        ILogger<ConfirmEmailModel> logger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _logger = logger;
    }

    public string StatusMessage { get; private set; } = "";

    public string ReturnUrl { get; private set; } = DefaultReturnUrl;

    public bool HasError { get; private set; }

    public async Task<IActionResult> OnGetAsync(string? userId, string? code, string? returnUrl = null)
    {
        ReturnUrl = GetSafeReturnUrl(returnUrl);

        if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(code))
        {
            return ShowError("This confirmation link is incomplete or invalid.");
        }

        var user = await _userManager.FindByIdAsync(userId);

        if (user is null)
        {
            return ShowError("This confirmation link could not be matched to an account.");
        }

        var alreadyConfirmed =
            await _userManager.IsEmailConfirmedAsync(user);

        if (alreadyConfirmed)
        {
            // Do not use an old confirmation link as a reusable
            // automatic-login link.
            return RedirectToPage("./Login", new { returnUrl = ReturnUrl });
        }

        string decodedToken;

        try
        {
            decodedToken = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(code));
        }
        catch (FormatException)
        {
            return ShowError("This confirmation link is invalid or has expired.");
        }

        var result = await _userManager.ConfirmEmailAsync(user, decodedToken);

        if (!result.Succeeded)
        {
            return ShowError("This confirmation link is invalid or has expired.");
        }

        // Prevent a new account from inheriting unfinished profile-wizard
        // information left in the same browser session.
        HttpContext.Session.Clear();

        // Replace any existing authentication cookie with the newly
        // confirmed account.
        await _signInManager.SignOutAsync();

        await _signInManager.SignInAsync(user, isPersistent: false);

        _logger.LogInformation("A user confirmed their account and was signed in.");

        return LocalRedirect(ReturnUrl);
    }

    private IActionResult ShowError(string message)
    {
        HasError = true;
        StatusMessage = message;

        return Page();
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