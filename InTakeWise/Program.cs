using InTakeWise.Data;
using InTakeWise.Filters;
using InTakeWise.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using InTakeWise.Middleware;
using InTakeWise.Security;
using Microsoft.AspNetCore.RateLimiting;
using OpenAI.Chat;
using System.Security.Claims;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

if (builder.Environment.IsDevelopment())
{
    builder.Configuration.AddUserSecrets<Program>();
}

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString, sql => sql.EnableRetryOnFailure()));

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

            policy.RequireAssertion(context => !context.User.HasClaim(DbSeeder.DemoClaimType, DbSeeder.DemoClaimValue));
        });
});

builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add<RequireProfileCompletedAttribute>();
});

builder.Services.AddRazorPages(options =>
{
    options.Conventions.AuthorizeAreaFolder("Identity", "/Account/Manage", DemoPolicies.NonDemoIdentityManagement);
});

builder.Services.AddScoped<IFoodItemService, FoodItemService>();
builder.Services.AddScoped<IShoppingSuggestionService, ShoppingSuggestionService>();
builder.Services.AddScoped<IMealSuggestionService, MealSuggestionService>();
builder.Services.AddScoped<ILogEntryService, LogEntryService>();
builder.Services.AddSingleton<IAppClock, AppClock>();
builder.Services.AddScoped<IPantryUnitService, PantryUnitService>();
builder.Services.AddScoped<IDemoAiGuard, DemoAiGuard>();
builder.Services.AddSingleton<ReceiptVisionClient>();
builder.Services.AddScoped<IReceiptImageAnalyzer, OpenAiReceiptImageAnalyzer>();

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = async (context, cancellationToken) =>
    {
        context.HttpContext.Response.ContentType = "text/plain";
        await context.HttpContext.Response.WriteAsync(
            "Too many receipt-analysis attempts. Please wait a few minutes and try again.",
            cancellationToken);
    };

    options.AddPolicy("receipt-analysis", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? httpContext.Connection.RemoteIpAddress?.ToString()
                ?? "anonymous",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = 5,
                QueueLimit = 0,
                Window = TimeSpan.FromMinutes(10)
            }));
});

builder.Services.AddSingleton(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    var apiKey = config["OpenAI:ApiKey"] ?? Environment.GetEnvironmentVariable("OPENAI_API_KEY");

    if (string.IsNullOrWhiteSpace(apiKey))
        throw new InvalidOperationException("OPENAI_API_KEY is missing.");

    var model = config["OpenAI:LoggingModel"] ?? "gpt-5.1";
    return new ChatClient(model: model, apiKey: apiKey);
});

builder.Services.AddScoped<IAiLogParser, OpenAiLogParser>();

builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();

    try
    {
        var db = services.GetRequiredService<ApplicationDbContext>();

        if (app.Configuration.GetValue<bool>("RunMigrationsOnStartup"))
        {
            await db.Database.MigrateAsync();
        }

        var demoEnabled = app.Configuration.GetValue<bool>("Demo:Enabled");

        await DbSeeder.ConfigureDemoUserAsync(services, demoEnabled);

        if (demoEnabled)
        {
            await DemoDataSeeder.SeedAsync(services);
        }
    }
    catch (Exception ex)
    {
        logger.LogCritical(ex, "Startup migration or demo-account configuration faile");

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
app.UseRateLimiter();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.MapRazorPages().WithStaticAssets();

app.Run();
