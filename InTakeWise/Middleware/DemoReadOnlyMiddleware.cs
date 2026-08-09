using System.Security.Claims;
using InTakeWise.Data;

namespace InTakeWise.Middleware;

public sealed class DemoReadOnlyMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<DemoReadOnlyMiddleware> _logger;

    public DemoReadOnlyMiddleware(
        RequestDelegate next,
        ILogger<DemoReadOnlyMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (!IsDemoUser(context.User) ||
            IsSafeMethod(context.Request.Method) ||
            IsLogoutPost(context))
        {
            await _next(context);
            return;
        }

        _logger.LogWarning(
            "Blocked demo write request: {Method} {Path}",
            context.Request.Method,
            context.Request.Path);

        context.Response.StatusCode =
            StatusCodes.Status403Forbidden;

        context.Response.ContentType =
            "application/problem+json";

        context.Response.Headers["Cache-Control"] = "no-store";

        await context.Response.WriteAsJsonAsync(new
        {
            type = "about:blank",
            title = "Demo mode is read-only",
            status = StatusCodes.Status403Forbidden,
            detail = "Changes and live generation are disabled for the demo account."
        });
    }

    private static bool IsDemoUser(ClaimsPrincipal user)
    {
        return user.Identity?.IsAuthenticated == true &&
               user.HasClaim(claim =>
                   claim.Type == DbSeeder.DemoClaimType &&
                   claim.Value == DbSeeder.DemoClaimValue);
    }

    private static bool IsSafeMethod(string method)
    {
        return HttpMethods.IsGet(method) ||
               HttpMethods.IsHead(method) ||
               HttpMethods.IsOptions(method);
    }

    private static bool IsLogoutPost(HttpContext context)
    {
        if (!HttpMethods.IsPost(context.Request.Method))
        {
            return false;
        }

        var area =
            context.Request.RouteValues["area"]?.ToString();

        var page =
            context.Request.RouteValues["page"]?.ToString();

        return string.Equals(
                   area,
                   "Identity",
                   StringComparison.OrdinalIgnoreCase) &&
               string.Equals(
                   page,
                   "/Account/Logout",
                   StringComparison.OrdinalIgnoreCase);
    }
}