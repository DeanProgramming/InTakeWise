using InTakeWise.Data;
using InTakeWise.Dto;
using InTakeWise.Models;
using InTakeWise.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using System.Collections.Concurrent;
using System.Data.Common;
using LegacyEmailSender = Microsoft.AspNetCore.Identity.UI.Services.IEmailSender;

namespace InTakeWise.Tests.Infrastructure;

public sealed class InTakeWiseWebApplicationFactory : WebApplicationFactory<Program>
{
    public const string TestEmail = "smoke@example.test";
    public const string TestPassword = "Test123!";

    private readonly SqliteConnection _connection = new(
        "Data Source=:memory:;Foreign Keys=True");

    public InTakeWiseWebApplicationFactory()
    {
        _connection.Open();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Demo:Enabled"] = "false",
                ["RunMigrationsOnStartup"] = "false",
                ["OpenAI:ApiKey"] = "not-used-by-tests"
            });
        });

        builder.ConfigureServices(services =>
        {
            var dbContextDescriptor = services.SingleOrDefault(descriptor =>
                descriptor.ServiceType == typeof(IDbContextOptionsConfiguration<ApplicationDbContext>));

            if (dbContextDescriptor is not null)
            {
                services.Remove(dbContextDescriptor);
            }

            services.RemoveAll<DbConnection>();
            services.AddSingleton<DbConnection>(_connection);
            services.AddDbContext<ApplicationDbContext>((provider, options) =>
                options.UseSqlite(provider.GetRequiredService<DbConnection>()));

            services.RemoveAll<IAiLogParser>();
            services.AddSingleton<IAiLogParser>(new StubAiLogParser());

            services.RemoveAll<IAppClock>();
            services.AddSingleton<IAppClock>(new TestAppClock());

            services.RemoveAll<IShoppingSuggestionService>();
            services.AddSingleton<IShoppingSuggestionService>(new StubShoppingSuggestionService());

            services.RemoveAll<IEmailSender<IdentityUser>>();
            services.AddSingleton<IEmailSender<IdentityUser>>(
                new NoOpIdentityEmailSender());

            services.RemoveAll<LegacyEmailSender>();
            services.AddSingleton<LegacyEmailSender>(
                new NoOpLegacyEmailSender());
        });
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        var host = base.CreateHost(builder);

        using var scope = host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        db.Database.EnsureCreated();

        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
        var user = new IdentityUser
        {
            UserName = TestEmail,
            Email = TestEmail,
            EmailConfirmed = true
        };
        var result = userManager.CreateAsync(user, TestPassword).GetAwaiter().GetResult();

        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(x => x.Description));
            throw new InvalidOperationException($"Could not seed smoke-test user: {errors}");
        }

        return host;
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing)
        {
            _connection.Dispose();
        }
    }

    private sealed class StubShoppingSuggestionService : IShoppingSuggestionService
    {
        private readonly ConcurrentDictionary<string, ShoppingPlanDto> _plans = new();

        public Task<ShoppingPlanDto> GenerateWeekPlanAsync(
            string userId,
            List<UserFoodItemDto> currentInHouse,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            return Task.FromResult(new ShoppingPlanDto
            {
                ShoppingList =
                [
                    new ShoppingLineDto
                    {
                        Name = "Smoke-test chicken",
                        Quantity = 1m,
                        Unit = "kg"
                    }
                ],
                WeekMealsSummary =
                [
                    new WeeklyMealDto
                    {
                        Day = "Saturday",
                        MealDateLocal = new DateTime(2026, 8, 15),
                        Title = "Breakfast: Smoke-test meal plan",
                        Calories = 2_200,
                        ProteinGrams = 180,
                        CarbsGrams = 230,
                        FatGrams = 65,
                        MealDetails = new DailyMealDetailsDto()
                    }
                ]
            });
        }

        public Task SaveWeekPlanAsync(
            string userId,
            ShoppingPlanDto plan,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _plans[userId] = plan;
            return Task.CompletedTask;
        }

        public Task<ShoppingPlanDto?> GetSavedWeekPlanAsync(
            string userId,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _plans.TryGetValue(userId, out var plan);
            return Task.FromResult(plan);
        }
    }
}

