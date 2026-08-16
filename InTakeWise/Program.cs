using InTakeWise.Data;
using InTakeWise.Filters;
using InTakeWise.Middleware;
using InTakeWise.Security;
using InTakeWise.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

if (builder.Environment.IsDevelopment())
{
    builder.Configuration.AddUserSecrets<Program>();
}

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(
        connectionString,
        sql => sql.EnableRetryOnFailure()));

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddDefaultIdentity<IdentityUser>(options =>
{
    options.SignIn.RequireConfirmedAccount = true;

    options.Lockout.AllowedForNewUsers = true;
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
})
.AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(
        DemoPolicies.NonDemoIdentityManagement,
        policy =>
        {
            policy.RequireAuthenticatedUser();

            policy.RequireAssertion(context =>
                !context.User.HasClaim(
                    DbSeeder.DemoClaimType,
                    DbSeeder.DemoClaimValue));
        });
});

builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add<RequireProfileCompletedAttribute>();
});

builder.Services.AddRazorPages(options =>
{
    options.Conventions.AuthorizeAreaFolder(
        "Identity",
        "/Account/Manage",
        DemoPolicies.NonDemoIdentityManagement);
});

builder.Services.AddScoped<IFoodItemService, FoodItemService>();
builder.Services.AddScoped<IShoppingSuggestionService, ShoppingSuggestionService>();
builder.Services.AddScoped<IMealSuggestionService, MealSuggestionService>();
builder.Services.AddScoped<ILogEntryService, LogEntryService>();

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<IAppClock, AppClock>();
builder.Services.AddSingleton<INutritionTargetCalculator, NutritionTargetCalculator>();

builder.Services
    .AddOptions<AiSafetyOptions>()
    .Bind(builder.Configuration.GetSection(AiSafetyOptions.SectionName))
    .Validate(options => options.IsValid(), "AiSafety configuration contains an invalid timeout or request limit.")
    .ValidateOnStart();

builder.Services.AddSingleton<IAiRequestGate, AiRequestGate>();

// Keep provider clients lazy: demo pages use seeded, pre-generated results and
// must remain available when OpenAI is unavailable or not configured.
builder.Services.AddSingleton<IOpenAiChatClientProvider, OpenAiChatClientProvider>();
builder.Services.AddSingleton<IAiLogResponseParser, AiLogResponseParser>();
builder.Services.AddSingleton<IShoppingPlanResponseParser, ShoppingPlanResponseParser>();

builder.Services.AddScoped<IPantryUnitService, PantryUnitService>();
builder.Services.AddScoped<IDemoAiGuard, DemoAiGuard>();
builder.Services.AddScoped<IReceiptImageAnalyzer, OpenAiReceiptImageAnalyzer>();

builder.Services.AddScoped<IAiLogParser, OpenAiLogParser>();

builder.Services.AddDistributedMemoryCache();

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

var app = builder.Build();

if (!app.Environment.IsEnvironment("Testing"))
{
    using var scope = app.Services.CreateScope();

    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();

    try
    {
        var db = services.GetRequiredService<ApplicationDbContext>();

        if (app.Configuration.GetValue<bool>("RunMigrationsOnStartup"))
        {
            await db.Database.MigrateAsync();
        }

        var demoEnabled =
            app.Configuration.GetValue<bool>("Demo:Enabled");

        await DbSeeder.ConfigureDemoUserAsync(services, demoEnabled);

        if (demoEnabled)
        {
            await DemoDataSeeder.SeedAsync(services);
        }
    }
    catch (Exception ex)
    {
        logger.LogCritical(ex, "Startup migration or demo-account configuration failed");

        throw;
    }
}

if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseSession();

app.UseAuthentication();
app.UseAuthorization();

app.UseMiddleware<DemoReadOnlyMiddleware>();

app.MapStaticAssets();

app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.MapRazorPages()
    .WithStaticAssets();

app.Run();

public partial class Program
{
}