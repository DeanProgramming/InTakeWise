using System.Security.Claims;
using InTakeWise.Data;
using InTakeWise.Middleware;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;

namespace InTakeWise.Tests.Unit;

public sealed class DemoReadOnlyMiddlewareTests
{
    [Theory]
    [InlineData("POST")]
    [InlineData("PUT")]
    [InlineData("PATCH")]
    [InlineData("DELETE")]
    public async Task DemoUser_UnsafeRequestIsBlockedWithProblemDetails(string method)
    {
        var nextWasCalled = false;
        var middleware = Middleware(_ =>
        {
            nextWasCalled = true;
            return Task.CompletedTask;
        });
        var context = Context(method, demoUser: true);

        await middleware.InvokeAsync(context);

        Assert.False(nextWasCalled);
        Assert.Equal(StatusCodes.Status403Forbidden, context.Response.StatusCode);
        Assert.StartsWith("application/problem+json", context.Response.ContentType);

        context.Response.Body.Position = 0;
        var body = await new StreamReader(context.Response.Body).ReadToEndAsync();
        Assert.Contains("Demo mode is read-only", body);
    }

    [Fact]
    public async Task DemoUser_LogoutPostIsAllowed()
    {
        var nextWasCalled = false;
        var middleware = Middleware(_ =>
        {
            nextWasCalled = true;
            return Task.CompletedTask;
        });
        var context = Context(HttpMethods.Post, demoUser: true);
        context.Request.RouteValues["area"] = "Identity";
        context.Request.RouteValues["page"] = "/Account/Logout";

        await middleware.InvokeAsync(context);

        Assert.True(nextWasCalled);
    }

    private static DemoReadOnlyMiddleware Middleware(RequestDelegate next) =>
        new(next, NullLogger<DemoReadOnlyMiddleware>.Instance);

    private static DefaultHttpContext Context(string method, bool demoUser)
    {
        var context = new DefaultHttpContext();
        context.Request.Method = method;
        context.Response.Body = new MemoryStream();

        var claims = demoUser
            ? new[] { new Claim(DbSeeder.DemoClaimType, DbSeeder.DemoClaimValue) }
            : Array.Empty<Claim>();

        context.User = new ClaimsPrincipal(
            new ClaimsIdentity(claims, authenticationType: "Test"));

        return context;
    }
}
