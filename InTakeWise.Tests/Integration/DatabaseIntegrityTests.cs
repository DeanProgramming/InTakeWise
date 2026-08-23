using InTakeWise.Helper;
using InTakeWise.Models;
using InTakeWise.Tests.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace InTakeWise.Tests.Integration;

public sealed class DatabaseIntegrityTests
{
    [Fact]
    public async Task FoodItem_NormalizedNameMustBeUnique()
    {
        using var database = new SqliteTestDatabase();
        await using var db = database.CreateContext();
        db.FoodItems.AddRange(
            new FoodItem { Name = "Oats", NormalizedName = "oats" },
            new FoodItem { Name = "OATS", NormalizedName = "oats" });

        await Assert.ThrowsAsync<DbUpdateException>(() =>
            db.SaveChangesAsync());
    }

    [Fact]
    public async Task MealSlot_UserDateAndMealTypeMustBeUnique()
    {
        using var database = new SqliteTestDatabase();
        await using var db = database.CreateContext();
        await AddUserAsync(db, "user-a");
        var localDate = new DateTime(2026, 8, 22);
        db.MealLogs.AddRange(
            Meal("user-a", localDate, new DateTime(2026, 8, 22, 8, 0, 0, DateTimeKind.Utc)),
            Meal("user-a", localDate, new DateTime(2026, 8, 22, 9, 0, 0, DateTimeKind.Utc)));

        await Assert.ThrowsAsync<DbUpdateException>(() =>
            db.SaveChangesAsync());
    }

    [Fact]
    public async Task ShoppingList_AllowsOnlyOneSavedPlanPerUser()
    {
        using var database = new SqliteTestDatabase();
        await using var db = database.CreateContext();
        await AddUserAsync(db, "user-a");
        db.ShoppingLists.AddRange(
            List("user-a", new DateTime(2026, 8, 24)),
            List("user-a", new DateTime(2026, 8, 31)));

        await Assert.ThrowsAsync<DbUpdateException>(() =>
            db.SaveChangesAsync());
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

    private static MealLogEntry Meal(
        string userId,
        DateTime localDate,
        DateTime timestamp) => new()
        {
            UserId = userId,
            TimeEat = MealType.Breakfast,
            LogDateLocal = localDate,
            Timestamp = timestamp,
            Calories = 500
        };

    private static ShoppingList List(string userId, DateTime weekStart) => new()
    {
        UserId = userId,
        CreatedAt = new DateTime(2026, 8, 22, 12, 0, 0, DateTimeKind.Utc),
        WeekStartLocalDate = weekStart
    };

}
