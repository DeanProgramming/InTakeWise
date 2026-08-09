using InTakeWise.Data;
using InTakeWise.Filters;
using InTakeWise.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using InTakeWise.Middleware;
using InTakeWise.Security;
using OpenAI.Chat;

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
})
.AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add<RequireProfileCompletedAttribute>();
});

builder.Services.AddRazorPages();

builder.Services.AddScoped<IFoodItemService, FoodItemService>();
builder.Services.AddScoped<IShoppingSuggestionService, ShoppingSuggestionService>();
builder.Services.AddScoped<IMealSuggestionService, MealSuggestionService>();
builder.Services.AddScoped<ILogEntryService, LogEntryService>();
builder.Services.AddSingleton<IAppClock, AppClock>();
builder.Services.AddScoped<IPantryUnitService, PantryUnitService>();
builder.Services.AddScoped<IDemoAiGuard, DemoAiGuard>();

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

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.MapRazorPages().WithStaticAssets();

app.Run();