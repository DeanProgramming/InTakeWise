using InTakeWise.Dto;
using InTakeWise.Helper;
using InTakeWise.Models;
using InTakeWise.Services;
using InTakeWise.Tests.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace InTakeWise.Tests.Integration;

public sealed class UserDataIsolationTests
{
    [Fact]
    public async Task PantryQueries_ReturnOnlyTheRequestedUsersItems()
    {
        using var database = new SqliteTestDatabase();
        await using var db = database.CreateContext();
        await AddUsersAsync(db, "user-a", "user-b");

        var oats = new FoodItem { Name = "Oats", NormalizedName = "oats" };
        var rice = new FoodItem { Name = "Rice", NormalizedName = "rice" };
        db.AddRange(oats, rice);
        await db.SaveChangesAsync();

        db.PantryItems.AddRange(
            new PantryItem { UserId = "user-a", FoodItemId = oats.Id, Quantity = 1m, Unit = "kg" },
            new PantryItem { UserId = "user-b", FoodItemId = rice.Id, Quantity = 2m, Unit = "kg" });
        await db.SaveChangesAsync();

        var service = new FoodItemService(db);
        var items = await service.GetFoodItemsAsync("user-a");

        var item = Assert.Single(items);
        Assert.Equal("Oats", item.Name);
        Assert.DoesNotContain(items, x => x.Name == "Rice");
    }

    [Fact]
    public async Task LogLookups_DoNotReturnAnotherUsersMealOrWorkout()
    {
        using var database = new SqliteTestDatabase();
        await using var db = database.CreateContext();
        await AddUsersAsync(db, "user-a", "user-b");
        var clock = new TestAppClock();

        var meal = new MealLogEntry
        {
            UserId = "user-a",
            TimeEat = MealType.Breakfast,
            Timestamp = clock.UtcNow,
            Calories = 500
        };
        var workout = new WorkoutLogEntry
        {
            UserId = "user-a",
            Timestamp = clock.UtcNow,
            ActivityType = "Run"
        };
        db.AddRange(meal, workout);
        await db.SaveChangesAsync();

        var service = new LogEntryService(db, new StubAiLogParser(), clock);

        Assert.Null(await service.GetMealByIdAsync(meal.Id, "user-b"));
        Assert.Null(await service.GetWorkoutByIdAsync(workout.Id, "user-b"));
        Assert.NotNull(await service.GetMealByIdAsync(meal.Id, "user-a"));
        Assert.NotNull(await service.GetWorkoutByIdAsync(workout.Id, "user-a"));
    }

    [Fact]
    public async Task DailySummary_AggregatesOnlyTheRequestedUsersLogs()
    {
        using var database = new SqliteTestDatabase();
        await using var db = database.CreateContext();
        await AddUsersAsync(db, "user-a", "user-b");
        var clock = new TestAppClock();

        db.UsersInformation.Add(Profile("user-a"));
        db.MealLogs.AddRange(
            Meal("user-a", MealType.Breakfast, clock.UtcNow, 500),
            Meal("user-b", MealType.Breakfast, clock.UtcNow, 4_000));
        db.WorkoutLogs.AddRange(
            Workout("user-a", clock.UtcNow, 200),
            Workout("user-b", clock.UtcNow, 2_000));
        await db.SaveChangesAsync();

        var service = new LogEntryService(db, new StubAiLogParser(), clock);
        var summary = await service.GetTodaySummaryAsync("user-a");

        Assert.NotNull(summary);
        Assert.Equal(500, summary.CaloriesEaten);
        Assert.Equal(200, summary.CaloriesBurned);
    }

    [Fact]
    public async Task LoggingMeal_ReplacesOnlyTheUsersExistingMealSlot()
    {
        using var database = new SqliteTestDatabase();
        await using var db = database.CreateContext();
        await AddUsersAsync(db, "user-a", "user-b");
        var clock = new TestAppClock();

        db.MealLogs.AddRange(
            Meal("user-a", MealType.Dinner, clock.UtcNow.AddHours(-1), 400),
            Meal("user-b", MealType.Dinner, clock.UtcNow.AddHours(-1), 900));
        await db.SaveChangesAsync();

        var parser = new StubAiLogParser
        {
            MealResult = new MealAnalysisDto
            {
                Summary = "Replacement",
                Calories = 650,
                Protein = 50,
                Carbs = 70,
                Fat = 18,
                Fiber = 8
            }
        };
        var service = new LogEntryService(db, parser, clock);

        await service.LogMealInfoAsync("user-a", "replacement meal", MealType.Dinner);

        var userAMeals = await db.MealLogs.Where(x => x.UserId == "user-a").ToListAsync();
        var userBMeals = await db.MealLogs.Where(x => x.UserId == "user-b").ToListAsync();

        var replacement = Assert.Single(userAMeals);
        Assert.Equal(650, replacement.Calories);
        Assert.Equal(900, Assert.Single(userBMeals).Calories);
    }

    private static async Task AddUsersAsync(
        InTakeWise.Data.ApplicationDbContext db,
        params string[] userIds)
    {
        db.Users.AddRange(userIds.Select(userId => new IdentityUser
        {
            Id = userId,
            UserName = $"{userId}@example.test",
            NormalizedUserName = $"{userId}@example.test".ToUpperInvariant(),
            Email = $"{userId}@example.test",
            NormalizedEmail = $"{userId}@example.test".ToUpperInvariant(),
            EmailConfirmed = true,
            SecurityStamp = Guid.NewGuid().ToString()
        }));

        await db.SaveChangesAsync();
    }

    private static UsersInformation Profile(string userId) => new()
    {
        UserId = userId,
        ProfileUserName = userId,
        Age = 26,
        Gender = Genders.Male,
        WeightInKg = 86,
        HeightInCM = 190,
        EveryDayFitnessLevel = FitnessLevel.Medium,
        ChosenGymDays = GymDays.Saturday,
        ChosenFitnessGoal = FitnessGoal.Maintain,
        CaloriesTargetGymDay = 2_500,
        ProteinTargetGymDay = 180,
        CarbsTargetGymDay = 300,
        FatTargetGymDay = 70,
        FiberTargetGymDay = 35,
        CaloriesTargetNonGymDay = 2_200,
        ProteinTargetNonGymDay = 180,
        CarbsTargetNonGymDay = 240,
        FatTargetNonGymDay = 70,
        FiberTargetNonGymDay = 30
    };

    private static MealLogEntry Meal(
        string userId,
        MealType mealType,
        DateTime timestamp,
        int calories) => new()
        {
            UserId = userId,
            TimeEat = mealType,
            Timestamp = timestamp,
            LogDateLocal = timestamp.Date,
            Calories = calories
        };

    private static WorkoutLogEntry Workout(string userId, DateTime timestamp, int calories) => new()
    {
        UserId = userId,
        Timestamp = timestamp,
        ActivityType = "Test workout",
        CaloriesBurned = calories
    };
}

