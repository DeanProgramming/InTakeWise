using System.Security.Claims;
using Microsoft.AspNetCore.Identity;

namespace InTakeWise.Data
{
    public static class DbSeeder
    {
        public const string DemoEmail = "Test@Test.com";
        public const string DemoClaimType = "account_type";
        public const string DemoClaimValue = "demo";

        public static async Task ConfigureDemoUserAsync(
            IServiceProvider services,
            bool demoEnabled)
        {
            var userManager =
                services.GetRequiredService<UserManager<IdentityUser>>();

            var logger = services
                .GetRequiredService<ILoggerFactory>()
                .CreateLogger("DbSeeder");

            var user =
                await userManager.FindByNameAsync(DemoEmail) ??
                await userManager.FindByEmailAsync(DemoEmail);

            var existingUserChanged = false;

            if (user is not null && await userManager.HasPasswordAsync(user))
            {
                EnsureSucceeded(
                    await userManager.RemovePasswordAsync(user),
                    "removing the legacy demo password");

                existingUserChanged = true;
                logger.LogInformation(
                    "Removed the legacy password from the demo account.");
            }

            if (!demoEnabled)
            {
                if (user is not null && existingUserChanged)
                {
                    EnsureSucceeded(
                        await userManager.UpdateSecurityStampAsync(user),
                        "invalidating old demo sessions");
                }

                logger.LogInformation("Demo account seeding is disabled.");
                return;
            }

            var created = false;

            if (user is null)
            {
                user = new IdentityUser
                {
                    UserName = DemoEmail,
                    Email = DemoEmail,
                    EmailConfirmed = true
                };


                EnsureSucceeded(
                    await userManager.CreateAsync(user),
                    "creating the passwordless demo user");

                created = true;
            }

            if (!user.EmailConfirmed)
            {
                user.EmailConfirmed = true;

                EnsureSucceeded(
                    await userManager.UpdateAsync(user),
                    "confirming the demo email");

                existingUserChanged = true;
            }

            var accountTypeClaims = (await userManager.GetClaimsAsync(user))
                .Where(claim => claim.Type == DemoClaimType)
                .ToList();

            var claimNeedsUpdating =
                accountTypeClaims.Count != 1 ||
                accountTypeClaims[0].Value != DemoClaimValue;

            if (claimNeedsUpdating)
            {
                if (accountTypeClaims.Count > 0)
                {
                    EnsureSucceeded(
                        await userManager.RemoveClaimsAsync(
                            user,
                            accountTypeClaims),
                        "removing stale account-type claims");
                }

                EnsureSucceeded(
                    await userManager.AddClaimAsync(
                        user,
                        new Claim(DemoClaimType, DemoClaimValue)),
                    "adding the demo account claim");

                existingUserChanged = true;
            }

            if (!created && existingUserChanged)
            {
                EnsureSucceeded(
                    await userManager.UpdateSecurityStampAsync(user),
                    "invalidating old demo sessions");
            }

            logger.LogInformation(
                created
                    ? "Created the passwordless demo user."
                    : "The passwordless demo user is configured.");
        }

        private static void EnsureSucceeded(
            IdentityResult result,
            string operation)
        {
            if (result.Succeeded)
            {
                return;
            }

            var errors = string.Join(
                ", ",
                result.Errors.Select(error =>
                    $"{error.Code}: {error.Description}"));

            throw new InvalidOperationException(
                $"Failed while {operation}. {errors}");
        }
    }
}