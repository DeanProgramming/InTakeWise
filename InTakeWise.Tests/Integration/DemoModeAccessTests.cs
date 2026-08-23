using System.Net;
using System.Security.Claims;
using System.Text.RegularExpressions;
using InTakeWise.Data;
using InTakeWise.Tests.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace InTakeWise.Tests.Integration;

public sealed class DemoModeAccessTests : IClassFixture<InTakeWiseWebApplicationFactory>
{
    private readonly InTakeWiseWebApplicationFactory _factory;

    public DemoModeAccessTests(InTakeWiseWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task DemoSession_StaysInDemoUntilEndDemoIsPosted()
    {
        await MarkTestUserAsDemoAsync();

        using var client = _factory.CreateClient(
            new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false,
                HandleCookies = true,
                BaseAddress = new Uri("https://localhost")
            });

        await SignInAsync(client);

        string[] restrictedPaths =
        [
            "/Identity/Account/Login",
            "/Identity/Account/Register",
            "/Identity/Account/ConfirmEmail",
            "/Identity/Account/ForgotPassword",
            "/Identity/Account/Manage/Index",
            "/Identity/Account/Logout",
            "/ProfileWizard/Step1",
            "/ProfileWizard/Step2",
            "/ProfileWizard/Step3"
        ];

        foreach (var path in restrictedPaths)
        {
            using var response = await client.GetAsync(path);

            Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
            Assert.Equal("/", response.Headers.Location?.OriginalString);
            Assert.True(response.Headers.CacheControl?.NoStore == true);
        }

        using var home = await client.GetAsync("/");
        home.EnsureSuccessStatusCode();

        var homeHtml = await home.Content.ReadAsStringAsync();
        Assert.Contains("End demo", homeHtml);
        Assert.Contains("/Identity/Account/Logout", homeHtml);

        var logoutToken = AntiforgeryToken(homeHtml);

        using var logout = await client.PostAsync("/Identity/Account/Logout?returnUrl=%2FIdentity%2FAccount%2FLogin",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = logoutToken
            }));

        Assert.Equal(HttpStatusCode.Redirect, logout.StatusCode);
        Assert.Equal("/Identity/Account/Login", logout.Headers.Location?.OriginalString);

        using var loginAfterDemo = await client.GetAsync("/Identity/Account/Login");

        Assert.Equal(HttpStatusCode.OK, loginAfterDemo.StatusCode);
    }

    private async Task MarkTestUserAsDemoAsync()
    {
        using var scope = _factory.Services.CreateScope();

        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();

        var user = await userManager.FindByEmailAsync(InTakeWiseWebApplicationFactory.TestEmail);

        Assert.NotNull(user);

        var claims = await userManager.GetClaimsAsync(user);

        if (claims.Any(claim => claim.Type == DbSeeder.DemoClaimType && claim.Value == DbSeeder.DemoClaimValue))
        {
            return;
        }

        var result = await userManager.AddClaimAsync(user, new Claim(DbSeeder.DemoClaimType, DbSeeder.DemoClaimValue));

        Assert.True(result.Succeeded, string.Join(", ", result.Errors.Select(error => error.Description)));
    }

    private static async Task SignInAsync(HttpClient client)
    {
        using var loginPage = await client.GetAsync("/Identity/Account/Login");

        Assert.True(loginPage.Headers.CacheControl?.NoStore == true);

        var loginHtml = await loginPage.Content.ReadAsStringAsync();
        var loginToken = AntiforgeryToken(loginHtml);

        using var login = await client.PostAsync(
            "/Identity/Account/Login",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = loginToken,
                ["Input.Email"] = InTakeWiseWebApplicationFactory.TestEmail,
                ["Input.Password"] = InTakeWiseWebApplicationFactory.TestPassword,
                ["Input.RememberMe"] = "false"
            }));

        Assert.Equal(HttpStatusCode.Redirect, login.StatusCode);
    }

    private static string AntiforgeryToken(string html)
    {
        var input = Regex.Match(html, "<input[^>]*name=[\"']__RequestVerificationToken[\"'][^>]*>", RegexOptions.IgnoreCase);

        var value = Regex.Match(input.Value, "value=[\"']([^\"']+)[\"']", RegexOptions.IgnoreCase);

        Assert.True(value.Success, "The page did not contain an antiforgery token.");

        return WebUtility.HtmlDecode(value.Groups[1].Value);
    }
}


