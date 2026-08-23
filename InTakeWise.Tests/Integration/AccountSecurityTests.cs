using System.Net;
using System.Text.RegularExpressions;
using InTakeWise.Data;
using InTakeWise.Tests.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace InTakeWise.Tests.Integration;

public sealed class AccountSecurityTests : IClassFixture<InTakeWiseWebApplicationFactory>
{
    private readonly InTakeWiseWebApplicationFactory _factory;

    public AccountSecurityTests(InTakeWiseWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Register_RejectsReservedDemoAddress()
    {
        using var client = Client();
        const string path = "/Identity/Account/Register?returnUrl=%2FProfileWizard%2FStep1";
        var token = await AntiforgeryTokenAsync(await client.GetAsync(path));

        using var response = await PostFormAsync(client, path, token, new()
        {
            ["Input.Email"] = DbSeeder.DemoEmail,
            ["Input.Password"] = InTakeWiseWebApplicationFactory.TestPassword,
            ["Input.ConfirmPassword"] = InTakeWiseWebApplicationFactory.TestPassword
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains("That email address is unavailable.", html);

        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
        Assert.Null(await userManager.FindByEmailAsync(DbSeeder.DemoEmail));
    }

    [Fact]
    public async Task Register_ConfirmationLinkConfirmsAndSignsInOnlyOnce()
    {
        using var client = Client();
        var email = $"register-{Guid.NewGuid():N}@example.test";
        const string registerPath =
            "/Identity/Account/Register?returnUrl=%2FProfileWizard%2FStep1";
        var token = await AntiforgeryTokenAsync(await client.GetAsync(registerPath));

        using var registered = await PostFormAsync(client, registerPath, token, new()
        {
            ["Input.Email"] = email,
            ["Input.Password"] = InTakeWiseWebApplicationFactory.TestPassword,
            ["Input.ConfirmPassword"] = InTakeWiseWebApplicationFactory.TestPassword
        });

        Assert.Equal(HttpStatusCode.Redirect, registered.StatusCode);
        Assert.StartsWith(
            "/Identity/Account/RegisterConfirmation",
            registered.Headers.Location?.OriginalString);

        using var confirmationPage = await client.GetAsync(registered.Headers.Location);
        confirmationPage.EnsureSuccessStatusCode();
        var confirmationHtml = await confirmationPage.Content.ReadAsStringAsync();
        var confirmationUrl = LinkById(confirmationHtml, "confirm-account-button");

        using var confirmed = await client.GetAsync(confirmationUrl);

        Assert.Equal(HttpStatusCode.Redirect, confirmed.StatusCode);
        Assert.Equal("/ProfileWizard/Step1", confirmed.Headers.Location?.OriginalString);

        using (var scope = _factory.Services.CreateScope())
        {
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
            var user = await userManager.FindByEmailAsync(email);

            Assert.NotNull(user);
            Assert.True(await userManager.IsEmailConfirmedAsync(user));
        }

        using var freshClient = Client();
        using var reused = await freshClient.GetAsync(confirmationUrl);

        Assert.Equal(HttpStatusCode.Redirect, reused.StatusCode);
        Assert.StartsWith(
            "/Identity/Account/Login",
            reused.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task Login_UsesLockoutAfterFiveFailedAttempts()
    {
        var email = $"lockout-{Guid.NewGuid():N}@example.test";
        await CreateConfirmedUserAsync(email);
        using var client = Client();

        for (var attempt = 1; attempt <= 5; attempt++)
        {
            const string path = "/Identity/Account/Login";
            var token = await AntiforgeryTokenAsync(await client.GetAsync(path));
            using var response = await PostFormAsync(client, path, token, new()
            {
                ["Input.Email"] = email,
                ["Input.Password"] = "Wrong123!",
                ["Input.RememberMe"] = "false"
            });

            if (attempt < 5)
            {
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            }
            else
            {
                Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
                Assert.StartsWith(
                    "/Identity/Account/Lockout",
                    response.Headers.Location?.OriginalString);
            }
        }

        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
        var user = await userManager.FindByEmailAsync(email);

        Assert.NotNull(user);
        Assert.True(await userManager.IsLockedOutAsync(user));
    }

    private HttpClient Client() =>
        _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true,
            BaseAddress = new Uri("https://localhost")
        });

    private async Task CreateConfirmedUserAsync(string email)
    {
        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
        var result = await userManager.CreateAsync(new IdentityUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true
        }, InTakeWiseWebApplicationFactory.TestPassword);

        Assert.True(
            result.Succeeded,
            string.Join(", ", result.Errors.Select(error => error.Description)));
    }

    private static async Task<HttpResponseMessage> PostFormAsync(
        HttpClient client,
        string path,
        string antiforgeryToken,
        Dictionary<string, string> values)
    {
        values["__RequestVerificationToken"] = antiforgeryToken;
        return await client.PostAsync(path, new FormUrlEncodedContent(values));
    }

    private static async Task<string> AntiforgeryTokenAsync(
        HttpResponseMessage response)
    {
        using (response)
        {
            response.EnsureSuccessStatusCode();
            var html = await response.Content.ReadAsStringAsync();
            var input = Regex.Match(
                html,
                "<input[^>]*name=[\"']__RequestVerificationToken[\"'][^>]*>",
                RegexOptions.IgnoreCase);
            var value = Regex.Match(
                input.Value,
                "value=[\"']([^\"']+)[\"']",
                RegexOptions.IgnoreCase);

            Assert.True(value.Success, "The page did not contain an antiforgery token.");
            return WebUtility.HtmlDecode(value.Groups[1].Value);
        }
    }

    private static string LinkById(string html, string id)
    {
        var tag = Regex.Match(
            html,
            $"<a[^>]*id=[\"']{Regex.Escape(id)}[\"'][^>]*>",
            RegexOptions.IgnoreCase);
        var href = Regex.Match(
            tag.Value,
            "href=[\"']([^\"']+)[\"']",
            RegexOptions.IgnoreCase);

        Assert.True(href.Success, $"The page did not contain link '{id}'.");
        return WebUtility.HtmlDecode(href.Groups[1].Value);
    }
}
