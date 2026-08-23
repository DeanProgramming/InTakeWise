using System.Net;
using System.Text.RegularExpressions;
using InTakeWise.Data;
using InTakeWise.Models;
using InTakeWise.Tests.Infrastructure;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace InTakeWise.Tests.Smoke;

public sealed class UserJourneySmokeTests : IClassFixture<InTakeWiseWebApplicationFactory>
{
    private readonly InTakeWiseWebApplicationFactory _factory;

    public UserJourneySmokeTests(InTakeWiseWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task UserCanLoginCompleteProfileLogMealAndGeneratePlan()
    {
        using var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true,
            BaseAddress = new Uri("https://localhost")
        });

        using var loginPage = await client.GetAsync(
            "/Identity/Account/Login?returnUrl=%2FProfileWizard%2FStep1");
        var loginToken = await AntiforgeryTokenAsync(loginPage);
        using var login = await PostFormAsync(
            client,
            "/Identity/Account/Login?returnUrl=%2FProfileWizard%2FStep1",
            loginToken,
            new Dictionary<string, string>
            {
                ["Input.Email"] = InTakeWiseWebApplicationFactory.TestEmail,
                ["Input.Password"] = InTakeWiseWebApplicationFactory.TestPassword,
                ["Input.RememberMe"] = "false"
            });
        Assert.Equal(HttpStatusCode.Redirect, login.StatusCode);

        using var step1Page = await client.GetAsync("/ProfileWizard/Step1");
        var step1Token = await AntiforgeryTokenAsync(step1Page);
        using var step1 = await PostFormAsync(
            client,
            "/ProfileWizard/Step1",
            step1Token,
            new Dictionary<string, string>
            {
                ["ProfileUserName"] = "Smoke User",
                ["Age"] = "26",
                ["Gender"] = "Male",
                ["HeightInCM"] = "190",
                ["WeightInKg"] = "86"
            });
        AssertRedirectsTo(step1, "/ProfileWizard/Step2");

        using var step2Page = await client.GetAsync("/ProfileWizard/Step2");
        var step2Token = await AntiforgeryTokenAsync(step2Page);
        using var step2 = await PostFormAsync(
            client,
            "/ProfileWizard/Step2",
            step2Token,
            new Dictionary<string, string>
            {
                ["EveryDayFitnessLevel"] = "Medium",
                ["ChosenGymDays"] = "5"
            });
        AssertRedirectsTo(step2, "/ProfileWizard/Step3");

        using var step3Page = await client.GetAsync("/ProfileWizard/Step3");
        var step3Token = await AntiforgeryTokenAsync(step3Page);
        using var completed = await PostFormAsync(
            client,
            "/ProfileWizard/Complete",
            step3Token,
            new Dictionary<string, string>
            {
                ["CurrentFitnessGoal"] = "LightCut"
            });
        Assert.Equal(HttpStatusCode.Redirect, completed.StatusCode);

        using var logPage = await client.GetAsync("/LogEntry?mode=Meal&meal=Breakfast");
        var logToken = await AntiforgeryTokenAsync(logPage);
        using var logged = await PostFormAsync(
            client,
            "/LogEntry/Meal",
            logToken,
            new Dictionary<string, string>
            {
                ["userInput"] = "chicken and rice",
                ["mode"] = "Meal",
                ["logTime"] = "Breakfast"
            });
        Assert.Equal(HttpStatusCode.Redirect, logged.StatusCode);

        using var planPage = await client.GetAsync("/ShoppingSuggestion");
        var planToken = await AntiforgeryTokenAsync(planPage);
        using var generated = await PostFormAsync(
            client,
            "/ShoppingSuggestion/GetShoppingList",
            planToken,
            new Dictionary<string, string>());
        Assert.Equal(HttpStatusCode.Redirect, generated.StatusCode);

        using var renderedPlan = await client.GetAsync("/ShoppingSuggestion");
        renderedPlan.EnsureSuccessStatusCode();
        var planHtml = await renderedPlan.Content.ReadAsStringAsync();
        Assert.Contains("Smoke-test chicken", planHtml);
        Assert.Contains("Smoke-test meal plan", planHtml);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var profile = await db.UsersInformation.SingleAsync();
        var meal = await db.MealLogs.SingleAsync();

        Assert.Equal(FitnessGoal.LightCut, profile.ChosenFitnessGoal);
        Assert.Equal("chicken and rice", meal.RawInput);
        Assert.Equal(600, meal.Calories);
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

    private static async Task<string> AntiforgeryTokenAsync(HttpResponseMessage response)
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

    private static void AssertRedirectsTo(HttpResponseMessage response, string expectedPath)
    {
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal(expectedPath, response.Headers.Location?.OriginalString);
    }
}


