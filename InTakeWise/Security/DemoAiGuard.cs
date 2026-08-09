using InTakeWise.Data;
using Microsoft.AspNetCore.Identity;

namespace InTakeWise.Security;

public interface IDemoAiGuard
{
    Task EnsureLiveAiAllowedAsync(string userId);
}

public sealed class DemoAiGuard : IDemoAiGuard
{
    private readonly UserManager<IdentityUser> _userManager;
    private readonly ILogger<DemoAiGuard> _logger;

    public DemoAiGuard(
        UserManager<IdentityUser> userManager,
        ILogger<DemoAiGuard> logger)
    {
        _userManager = userManager;
        _logger = logger;
    }

    public async Task EnsureLiveAiAllowedAsync(string userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new UnauthorizedAccessException(
                "A valid user is required to use live AI.");
        }

        var user = await _userManager.FindByIdAsync(userId);

        if (user is null)
        {
            throw new UnauthorizedAccessException(
                "The authenticated user could not be found.");
        }

        var claims = await _userManager.GetClaimsAsync(user);

        var isDemo = claims.Any(claim =>
            claim.Type == DbSeeder.DemoClaimType &&
            claim.Value == DbSeeder.DemoClaimValue);

        if (!isDemo)
        {
            return;
        }

        _logger.LogWarning(
            "Blocked live AI access for demo user {UserId}.",
            userId);

        throw new DemoAiAccessDeniedException();
    }
}

public sealed class DemoAiAccessDeniedException
    : InvalidOperationException
{
    public DemoAiAccessDeniedException()
        : base(
            "Live AI generation is disabled in demo mode. " +
            "Previously generated sample results are shown instead.")
    {
    }
}