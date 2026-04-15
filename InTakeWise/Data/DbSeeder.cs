using InTakeWise.Models;
using Microsoft.AspNetCore.Identity;

namespace InTakeWise.Data
{
    public static class DbSeeder
    {
        public static async Task SeedTestUserAsync(IServiceProvider services)
        {
            using var scope = services.CreateScope();

            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
            var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>()
                .CreateLogger("DbSeeder");

            const string email = "Test@Test.com";
            const string password = "Test123!";

            // Check by username first, then email
            var existingUser =
                await userManager.FindByNameAsync(email) ??
                await userManager.FindByEmailAsync(email);

            if (existingUser != null)
            {
                logger.LogInformation("Seed user already exists: {Email}", email);
                return;
            }

            var user = new IdentityUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true
            };

            var result = await userManager.CreateAsync(user, password);

            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => $"{e.Code}: {e.Description}"));
                logger.LogError("Failed to seed test user {Email}. Errors: {Errors}", email, errors);
                throw new InvalidOperationException($"Could not seed test user. {errors}");
            }

            logger.LogInformation("Seeded test user: {Email}", email);
        }
    }
}