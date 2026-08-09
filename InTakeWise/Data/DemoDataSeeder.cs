using System.Text.Json;
using InTakeWise.Dto;
using InTakeWise.Helper;
using InTakeWise.Models;
using InTakeWise.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace InTakeWise.Data;

public static class DemoDataSeeder
{
    private static readonly SemaphoreSlim SeedLock = new(1, 1);

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public static async Task SeedAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        await SeedLock.WaitAsync(cancellationToken);

        try
        {
            await SeedCoreAsync(services, cancellationToken);
        }
        finally
        {
            SeedLock.Release();
        }
    }

    private static async Task SeedCoreAsync(IServiceProvider services, CancellationToken cancellationToken)
    {
        var userManager = services.GetRequiredService<UserManager<IdentityUser>>();
        var db = services.GetRequiredService<ApplicationDbContext>();
        var clock = services.GetRequiredService<IAppClock>();
        var utcNow = clock.UtcNow;
        var todayLocal = clock.LondonNow.Date;
        var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger("DemoDataSeeder");

        var demoUser = await userManager.FindByNameAsync(DbSeeder.DemoEmail) ?? await userManager.FindByEmailAsync(DbSeeder.DemoEmail);

        if (demoUser is null)
        {
            throw new InvalidOperationException("The demo user must be configured before sample data is seeded.");
        }

        var claims = await userManager.GetClaimsAsync(demoUser);
        var hasDemoClaim = claims.Any(claim => claim.Type == DbSeeder.DemoClaimType && claim.Value == DbSeeder.DemoClaimValue);

        if (!hasDemoClaim || await userManager.HasPasswordAsync(demoUser))
        {
            throw new InvalidOperationException("Refused to seed sample data because the demo identity is not safely configured.");
        }

        var profile = await UpsertProfileAsync(db, demoUser.Id, cancellationToken);
        var foodMap = await GetOrCreateFoodsAsync(db, cancellationToken);

        await UpsertPantryAsync(db, demoUser.Id, foodMap, todayLocal,cancellationToken);

        await UpsertTodayMealsAsync(db, demoUser.Id, utcNow, cancellationToken);

        await UpsertRecentWorkoutAsync(db, demoUser.Id, profile.ChosenGymDays, todayLocal, utcNow, cancellationToken);

        await UpsertShoppingPlanAsync(db, demoUser.Id, profile, foodMap, todayLocal, utcNow, cancellationToken);

        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Seeded current, idempotent sample data for the demo account.");
    }

    private static async Task<UsersInformation> UpsertProfileAsync(ApplicationDbContext db, string userId, CancellationToken cancellationToken)
    {
        var profile = await db.UsersInformation.SingleOrDefaultAsync(x => x.UserId == userId, cancellationToken);

        if (profile is null)
        {
            profile = new UsersInformation { UserId = userId };
            db.UsersInformation.Add(profile);
        }

        profile.ProfileUserName = "Demo Explorer";
        profile.Age = 32;
        profile.Gender = Genders.PreferNotToSay;
        profile.WeightInKg = 72;
        profile.HeightInCM = 175;
        profile.EveryDayFitnessLevel = FitnessLevel.Medium;
        profile.ChosenGymDays = GymDays.Monday | GymDays.Wednesday | GymDays.Friday;
        profile.ChosenFitnessGoal = FitnessGoal.Maintain;

        profile.CaloriesTargetGymDay = 2300;
        profile.ProteinTargetGymDay = 165;
        profile.CarbsTargetGymDay = 260;
        profile.FatTargetGymDay = 70;
        profile.FiberTargetGymDay = 30;

        profile.CaloriesTargetNonGymDay = 2100;
        profile.ProteinTargetNonGymDay = 165;
        profile.CarbsTargetNonGymDay = 220;
        profile.FatTargetNonGymDay = 70;
        profile.FiberTargetNonGymDay = 30;

        return profile;
    }

    private static readonly PantrySeed[] PantrySeeds =
    [
        new("Rolled oats", 750m, "g", 120, 370, 13, 60, 8, 10),
        new("Brown rice", 1.2m, "kg", 180, 360, 8, 76, 3, 4),
        new("Chicken breast", 600m, "g", 2, 120, 23, 0, 2, 0),
        new("Greek yoghurt", 500m, "g", 5, 73, 10, 4, 2, 0),
        new("Peanut butter", 250m, "g", 90, 588, 25, 20, 50, 6),
        new("Frozen mixed vegetables", 1m, "kg", 60, 55, 3, 8, 1, 5),
        new("Olive oil", 500m, "ml", 240, 884, 0, 0, 100, 0)
    ];

    private static async Task<Dictionary<string, FoodItem>> GetOrCreateFoodsAsync(ApplicationDbContext db, CancellationToken cancellationToken)
    {
        var normalizedNames = PantrySeeds
            .Select(x => FoodItemNameNormalizer.NormalizeName(x.Name))
            .ToList();

        var foods = await db.FoodItems
            .Where(x => normalizedNames.Contains(x.NormalizedName))
            .ToListAsync(cancellationToken);

        var foodMap = foods.ToDictionary(
            x => x.NormalizedName,
            StringComparer.Ordinal);

        foreach (var seed in PantrySeeds)
        {
            var normalizedName = FoodItemNameNormalizer.NormalizeName(seed.Name);

            if (foodMap.ContainsKey(normalizedName))
            {
                continue;
            }

            var food = new FoodItem
            {
                Name = FoodItemNameNormalizer.CleanDisplayName(seed.Name),
                NormalizedName = normalizedName,
                CaloriesPer100g = seed.CaloriesPer100g,
                ProteinPer100g = seed.ProteinPer100g,
                CarbsPer100g = seed.CarbsPer100g,
                FatPer100g = seed.FatPer100g,
                FiberPer100g = seed.FiberPer100g
            };

            db.FoodItems.Add(food);
            foodMap.Add(normalizedName, food);
        }

        return foodMap;
    }

    private static async Task UpsertPantryAsync(ApplicationDbContext db, string userId, IReadOnlyDictionary<string, FoodItem> foodMap, DateTime todayLocal, CancellationToken cancellationToken)
    {
        var existing = await db.PantryItems
            .Include(x => x.FoodItem)
            .Where(x => x.UserId == userId)
            .ToListAsync(cancellationToken);

        var retained = new HashSet<PantryItem>();

        foreach (var seed in PantrySeeds)
        {
            var normalizedName = FoodItemNameNormalizer.NormalizeName(seed.Name);
            var food = foodMap[normalizedName];
            var item = existing.FirstOrDefault(x =>
                x.FoodItem.NormalizedName == normalizedName);

            if (item is null)
            {
                item = new PantryItem
                {
                    UserId = userId,
                    FoodItem = food
                };

                db.PantryItems.Add(item);
            }
            else
            {
                item.FoodItem = food;
            }

            item.Quantity = seed.Quantity;
            item.Unit = seed.Unit;
            item.ExpiryDate = todayLocal.AddDays(seed.ExpiryDays);
            retained.Add(item);
        }

        db.PantryItems.RemoveRange(existing.Where(x => !retained.Contains(x)));
    }

    private static readonly MealLogSeed[] MealLogSeeds =
    [
        new(
            MealType.Breakfast,
            "Greek yoghurt with rolled oats, blueberries and a banana.",
            455, 31, 58, 12, 8),
        new(
            MealType.Dinner,
            "Chicken and brown rice bowl with mixed vegetables.",
            610, 52, 64, 16, 9),
        new(
            MealType.Tea,
            "Baked salmon with sweet potato and broccoli.",
            725, 56, 74, 22, 11),
        new(
            MealType.Snack,
            "Apple with peanut butter and Greek yoghurt.",
            285, 27, 30, 6, 5)
    ];

    private static async Task UpsertTodayMealsAsync( ApplicationDbContext db, string userId, DateTime timestampUtc, CancellationToken cancellationToken)
    {
        var existing = await db.MealLogs
            .Where(x => x.UserId == userId)
            .OrderBy(x => x.Id)
            .ToListAsync(cancellationToken);

        var retained = new HashSet<MealLogEntry>();

        foreach (var seed in MealLogSeeds)
        {
            var log = existing.FirstOrDefault(x => x.TimeEat == seed.TimeEat);

            if (log is null)
            {
                log = new MealLogEntry { UserId = userId };
                db.MealLogs.Add(log);
            }

            log.TimeEat = seed.TimeEat;
            log.Timestamp = timestampUtc;
            log.RawInput = seed.RawInput;
            log.Calories = seed.Calories;
            log.Protein = seed.Protein;
            log.Carbs = seed.Carbs;
            log.Fat = seed.Fat;
            log.Fiber = seed.Fiber;
            retained.Add(log);
        }

        db.MealLogs.RemoveRange(existing.Where(x => !retained.Contains(x)));
    }

    private static async Task UpsertRecentWorkoutAsync(ApplicationDbContext db, string userId, GymDays gymDays, DateTime todayLocal, DateTime utcNow, CancellationToken cancellationToken)
    {
        var existing = await db.WorkoutLogs
            .Where(x => x.UserId == userId)
            .OrderBy(x => x.Id)
            .ToListAsync(cancellationToken);

        var workout = existing.FirstOrDefault();

        if (workout is null)
        {
            workout = new WorkoutLogEntry { UserId = userId };
            db.WorkoutLogs.Add(workout);
        }

        var daysBack = Enumerable.Range(0, 7)
            .First(offset => IsGymDay(
                gymDays,
                todayLocal.AddDays(-offset).DayOfWeek));

        workout.Timestamp = utcNow.AddDays(-daysBack);
        workout.RawInput = "Full-body strength session: squats, dumbbell press, rows and a short bike cooldown.";
        workout.ActivityType = "Strength training";
        workout.DurationMinutes = 58;
        workout.Intensity = "Moderate";
        workout.CaloriesBurned = 360;

        db.WorkoutLogs.RemoveRange(existing.Skip(1));
    }

    private static async Task UpsertShoppingPlanAsync(
        ApplicationDbContext db,
        string userId,
        UsersInformation profile,
        IReadOnlyDictionary<string, FoodItem> foodMap,
        DateTime todayLocal,
        DateTime utcNow,
        CancellationToken cancellationToken)
    {
        var plan = await db.ShoppingLists
            .Include(x => x.Items)
            .Include(x => x.Meals)
            .SingleOrDefaultAsync(x => x.UserId == userId, cancellationToken);

        if (plan is null)
        {
            plan = new ShoppingList { UserId = userId };
            db.ShoppingLists.Add(plan);
        }
        else
        {
            db.ShoppingListItems.RemoveRange(plan.Items);
            db.ShoppingMealDays.RemoveRange(plan.Meals);
            plan.Items.Clear();
            plan.Meals.Clear();
        }

        var weekStart = todayLocal.AddDays(-DaysSinceMonday(todayLocal.DayOfWeek));

        plan.CreatedAt = utcNow;
        plan.WeekStartLocalDate = weekStart;

        foreach (var seed in ShoppingSeeds)
        {
            var normalizedName = FoodItemNameNormalizer.NormalizeName(seed.Name);
            foodMap.TryGetValue(normalizedName, out var food);

            plan.Items.Add(new ShoppingListItem
            {
                ShoppingList = plan,
                FoodItem = food,
                Name = seed.Name,
                Quantity = seed.Quantity,
                Unit = seed.Unit
            });
        }

        var dayPlans = BuildDayPlans();

        for (var offset = 0; offset < 7; offset++)
        {
            var date = weekStart.AddDays(offset);
            var seed = dayPlans[offset];
            var isGymDay = IsGymDay(profile.ChosenGymDays, date.DayOfWeek);

            plan.Meals.Add(new ShoppingMealDay
            {
                ShoppingList = plan,
                Day = date.DayOfWeek.ToString(),
                MealDateLocal = date,
                Title = seed.Title,
                MealDetailsJson = JsonSerializer.Serialize(seed.Details, JsonOptions),
                Calories = isGymDay ? profile.CaloriesTargetGymDay : profile.CaloriesTargetNonGymDay,
                ProteinGrams = isGymDay ? profile.ProteinTargetGymDay : profile.ProteinTargetNonGymDay,
                CarbsGrams = isGymDay ? profile.CarbsTargetGymDay : profile.CarbsTargetNonGymDay,
                FatGrams = isGymDay ? profile.FatTargetGymDay : profile.FatTargetNonGymDay,
                IsGymDay = isGymDay
            });
        }
    }

    private static readonly ShoppingSeed[] ShoppingSeeds =
    [
        new("Eggs", 12m, "each"),
        new("Salmon fillets", 4m, "each"),
        new("Wholegrain wraps", 1m, "pack"),
        new("Wholegrain bread", 1m, "loaf"),
        new("Sweet potatoes", 1.5m, "kg"),
        new("White potatoes", 2m, "kg"),
        new("Broccoli", 3m, "heads"),
        new("Green beans", 500m, "g"),
        new("Mixed berries", 500m, "g"),
        new("Bananas", 7m, "each"),
        new("Apples", 7m, "each"),
        new("Pears", 4m, "each"),
        new("Avocados", 2m, "each"),
        new("Peppers", 5m, "each"),
        new("Mushrooms", 300m, "g"),
        new("Salad leaves", 2m, "bags"),
        new("Tomatoes", 8m, "each"),
        new("Turkey mince", 750m, "g"),
        new("Lean beef strips", 500m, "g"),
        new("Tuna", 4m, "tins"),
        new("Cottage cheese", 500m, "g"),
        new("Wholewheat pasta", 500m, "g"),
        new("Lentils", 2m, "tins"),
        new("Kidney beans", 2m, "tins"),
        new("Hummus", 200m, "g"),
        new("Pineapple", 1m, "each"),
        new("Almonds", 200m, "g")
    ];

    private static IReadOnlyList<DayPlanSeed> BuildDayPlans()
    {
        return
        [
            DayPlan(
                "Breakfast: yoghurt berry oats; Lunch: chicken rice bowl; Dinner: salmon with sweet potato; Snack: apple and peanut butter",
                Meal("Greek yoghurt bowl with oats and berries.", ["200g Greek yoghurt", "60g rolled oats", "100g mixed berries"], ["Add everything to a bowl.", "Mix and serve."]),
                Meal("Chicken and vegetable brown rice bowl.", ["180g chicken breast", "150g cooked brown rice", "150g mixed vegetables"], ["Cook the chicken.", "Heat the rice and vegetables.", "Serve together."]),
                Meal("Baked salmon with sweet potato and broccoli.", ["180g salmon", "300g sweet potato", "150g broccoli"], ["Bake the salmon and sweet potato.", "Steam the broccoli.", "Plate and serve."]),
                Meal("Apple with peanut butter.", ["1 apple", "20g peanut butter"], ["Slice the apple.", "Serve with peanut butter."])),
            DayPlan(
                "Breakfast: eggs and avocado toast; Lunch: turkey wrap; Dinner: beef pepper rice; Snack: cottage cheese and pineapple",
                Meal("Eggs and avocado on wholegrain toast.", ["2 eggs", "1/2 avocado", "2 slices wholegrain bread"], ["Toast the bread.", "Cook the eggs.", "Top with avocado and eggs."]),
                Meal("Turkey and salad wrap.", ["150g turkey mince", "1 wholegrain wrap", "salad leaves", "tomato"], ["Cook the turkey.", "Fill the wrap with turkey and salad.", "Roll and serve."]),
                Meal("Beef and pepper stir-fry with brown rice.", ["170g lean beef strips", "1 pepper", "150g cooked brown rice"], ["Stir-fry the beef and pepper.", "Heat the rice.", "Serve together."]),
                Meal("Cottage cheese with pineapple.", ["180g cottage cheese", "150g pineapple"], ["Chop the pineapple.", "Serve with cottage cheese."])),
            DayPlan(
                "Breakfast: banana yoghurt oats; Lunch: tuna pasta salad; Dinner: chicken fajita bowl; Snack: yoghurt and berries",
                Meal("Warm oats with banana and Greek yoghurt.", ["60g rolled oats", "1 banana", "150g Greek yoghurt"], ["Cook the oats.", "Top with banana and yoghurt."]),
                Meal("Tuna wholewheat pasta salad.", ["1 tin tuna", "90g wholewheat pasta", "tomato", "salad leaves"], ["Cook and cool the pasta.", "Mix with tuna and salad."]),
                Meal("Chicken fajita rice bowl.", ["180g chicken breast", "1 pepper", "150g cooked brown rice", "1/2 avocado"], ["Cook the chicken and pepper.", "Add to rice.", "Top with avocado."]),
                Meal("Greek yoghurt and berries.", ["200g Greek yoghurt", "100g mixed berries"], ["Combine in a bowl."])),
            DayPlan(
                "Breakfast: apple cinnamon oats; Lunch: lentil soup and toast; Dinner: turkey pasta; Snack: pear and almonds",
                Meal("Apple and cinnamon overnight oats.", ["60g rolled oats", "1 apple", "150g Greek yoghurt"], ["Mix the oats and yoghurt.", "Top with chopped apple.", "Chill until needed."]),
                Meal("Tomato lentil soup with wholegrain toast.", ["1 tin lentils", "2 tomatoes", "2 slices wholegrain bread"], ["Simmer the lentils and tomatoes.", "Blend lightly.", "Serve with toast."]),
                Meal("Turkey meatballs with wholewheat pasta.", ["180g turkey mince", "90g wholewheat pasta", "2 tomatoes"], ["Shape and cook the meatballs.", "Cook the pasta.", "Make a quick tomato sauce and combine."]),
                Meal("Pear with almonds.", ["1 pear", "25g almonds"], ["Slice the pear.", "Serve with almonds."])),
            DayPlan(
                "Breakfast: eggs and mushrooms; Lunch: chicken rice bowl; Dinner: salmon potatoes and beans; Snack: banana yoghurt",
                Meal("Eggs, mushrooms and wholegrain toast.", ["2 eggs", "100g mushrooms", "2 slices wholegrain bread"], ["Cook the mushrooms and eggs.", "Toast the bread.", "Serve together."]),
                Meal("Chicken rice bowl with vegetables.", ["180g chicken breast", "150g cooked brown rice", "150g mixed vegetables"], ["Cook the chicken.", "Heat the rice and vegetables.", "Assemble the bowl."]),
                Meal("Salmon with potatoes and green beans.", ["180g salmon", "300g white potatoes", "150g green beans"], ["Bake the salmon.", "Boil the potatoes.", "Steam the beans."]),
                Meal("Banana with Greek yoghurt.", ["1 banana", "180g Greek yoghurt"], ["Slice the banana.", "Serve with yoghurt."])),
            DayPlan(
                "Breakfast: peanut butter overnight oats; Lunch: hummus chicken wrap; Dinner: turkey chilli; Snack: cottage cheese and berries",
                Meal("Peanut butter and banana overnight oats.", ["60g rolled oats", "1 banana", "20g peanut butter"], ["Mix the oats with water or milk.", "Add banana and peanut butter.", "Chill overnight."]),
                Meal("Hummus chicken salad wrap.", ["160g chicken breast", "1 wholegrain wrap", "40g hummus", "salad leaves"], ["Cook and slice the chicken.", "Spread hummus on the wrap.", "Add chicken and salad, then roll."]),
                Meal("Turkey and kidney bean chilli with brown rice.", ["180g turkey mince", "1/2 tin kidney beans", "2 tomatoes", "150g cooked brown rice"], ["Brown the turkey.", "Add beans and tomatoes, then simmer.", "Serve with rice."]),
                Meal("Cottage cheese with berries.", ["180g cottage cheese", "100g mixed berries"], ["Combine in a bowl."])),
            DayPlan(
                "Breakfast: vegetable omelette; Lunch: tuna jacket potato; Dinner: roast chicken and vegetables; Snack: apple yoghurt",
                Meal("Vegetable omelette with wholegrain toast.", ["3 eggs", "100g mixed vegetables", "1 slice wholegrain bread"], ["Cook the vegetables.", "Add beaten eggs and set the omelette.", "Serve with toast."]),
                Meal("Tuna jacket potato with salad.", ["1 tin tuna", "1 large white potato", "salad leaves", "tomato"], ["Bake the potato.", "Fill with tuna.", "Serve with salad."]),
                Meal("Roast chicken with potatoes and green beans.", ["200g chicken breast", "300g white potatoes", "150g green beans"], ["Roast the chicken and potatoes.", "Steam the green beans.", "Serve together."]),
                Meal("Apple with Greek yoghurt.", ["1 apple", "180g Greek yoghurt"], ["Chop the apple.", "Serve with yoghurt."]))
        ];
    }

    private static DayPlanSeed DayPlan(string title, MealDetailDto breakfast, MealDetailDto lunch, MealDetailDto dinner, MealDetailDto snack)
    {
        return new DayPlanSeed(
            title,
            new DailyMealDetailsDto
            {
                Breakfast = breakfast,
                Lunch = lunch,
                Dinner = dinner,
                Snack = snack,
                LateSnack = new MealDetailDto()
            });
    }

    private static MealDetailDto Meal(string overview, IEnumerable<string> ingredients, IEnumerable<string> steps)
    {
        return new MealDetailDto
        {
            Overview = overview,
            Ingredients = ingredients.ToList(),
            Steps = steps.ToList()
        };
    }

    private static int DaysSinceMonday(DayOfWeek dayOfWeek) => ((int)dayOfWeek - (int)DayOfWeek.Monday + 7) % 7;

    private static bool IsGymDay(GymDays gymDays, DayOfWeek dayOfWeek)
    {
        var flag = dayOfWeek switch
        {
            DayOfWeek.Monday => GymDays.Monday,
            DayOfWeek.Tuesday => GymDays.Tuesday,
            DayOfWeek.Wednesday => GymDays.Wednesday,
            DayOfWeek.Thursday => GymDays.Thursday,
            DayOfWeek.Friday => GymDays.Friday,
            DayOfWeek.Saturday => GymDays.Saturday,
            DayOfWeek.Sunday => GymDays.Sunday,
            _ => GymDays.None
        };

        return flag != GymDays.None && (gymDays & flag) != 0;
    }

    private sealed record PantrySeed(string Name, decimal Quantity, string Unit, int ExpiryDays, int CaloriesPer100g, int ProteinPer100g, int CarbsPer100g, int FatPer100g, int FiberPer100g);

    private sealed record MealLogSeed(MealType TimeEat, string RawInput, int Calories, int Protein, int Carbs, int Fat, int Fiber);

    private sealed record ShoppingSeed(string Name, decimal Quantity, string Unit);

    private sealed record DayPlanSeed(string Title, DailyMealDetailsDto Details);
}