using InTakeWise.Helper;
using InTakeWise.Models;
using InTakeWise.Services;
using InTakeWise.Tests.Infrastructure;
using Microsoft.AspNetCore.Identity;

namespace InTakeWise.Tests.Integration;

public sealed class LogEntryServiceTests
{
    [Fact]
    public async Task LogMeal_TrimsInputAndStoresLondonLocalDate()
    {
        using var database = new SqliteTestDatabase();
        await using var db = database.CreateContext();
        await AddUserAsync(db, "user-a");
        var clock = new TestAppClock(
            utcNow: new DateTime(2026, 8, 15, 23, 30, 0, DateTimeKind.Utc),
            londonNow: new DateTime(2026, 8, 16, 0, 30, 0));
        var parser = new StubAiLogParser();
        var service = new LogEntryService(db, parser, clock);

        var result = await service.LogMealInfoAsync(
            "user-a",
            "  chicken and rice  ",
            MealType.Dinner);

        Assert.Equal("chicken and rice", parser.LastMealInput);
        Assert.Equal("user-a", parser.LastMealUserId);
        Assert.Equal("chicken and rice", result.RawInput);
        Assert.Equal(new DateTime(2026, 8, 16), result.LogDateLocal);
        Assert.Equal(clock.UtcNow, result.Timestamp);
    }

    private static async Task AddUserAsync(
        InTakeWise.Data.ApplicationDbContext db,
        string id)
    {
        var email = $"{id}@example.test";
        db.Users.Add(new IdentityUser
        {
            Id = id,
            UserName = email,
            NormalizedUserName = email.ToUpperInvariant(),
            Email = email,
            NormalizedEmail = email.ToUpperInvariant(),
            EmailConfirmed = true,
            SecurityStamp = Guid.NewGuid().ToString()
        });
        await db.SaveChangesAsync();
    }

}
