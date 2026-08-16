using System.Text.Json;
using System.Text.Json.Serialization;
using InTakeWise.Dto;

namespace InTakeWise.Services;

public interface IShoppingPlanResponseParser
{
    ShoppingPlanDto Parse(
        string? json,
        IReadOnlyList<DailyTargetDto> expectedDays);
}

public sealed class ShoppingPlanResponseParser : IShoppingPlanResponseParser
{
    private const int MaximumShoppingLines = 200;
    private const int MaximumShoppingNameCharacters = 120;
    private const int MaximumUnitCharacters = 30;
    private const decimal MaximumShoppingQuantity = 100_000m;
    private const int MinimumDailyCalories = 500;
    private const int MaximumDailyCalories = 10_000;
    private const int MaximumMacroGrams = 1_000;
    private const int MaximumTitleCharacters = 1_500;
    private const int MaximumOverviewCharacters = 600;
    private const int MaximumIngredientsPerMeal = 40;
    private const int MaximumIngredientCharacters = 200;
    private const int MaximumStepsPerMeal = 30;
    private const int MaximumStepCharacters = 500;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow
    };

    public ShoppingPlanDto Parse(string? json, IReadOnlyList<DailyTargetDto> expectedDays)
    {
        if (expectedDays is null || expectedDays.Count is < 1 or > 7)
        {
            throw new ArgumentException("Expected shopping-plan days must contain between one and seven entries.", nameof(expectedDays));
        }

        var plan = Deserialize(json);

        plan.ShoppingList = NormalizeShoppingList(plan.ShoppingList);

        plan.WeekMealsSummary = NormalizeMealDays(plan.WeekMealsSummary, expectedDays);

        return plan;
    }

    private static ShoppingPlanDto Deserialize(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            throw new InvalidOperationException("AI returned an empty shopping-plan response.");
        }

        try
        {
            return JsonSerializer.Deserialize<ShoppingPlanDto>(json, JsonOptions) ?? throw new InvalidOperationException("AI returned an empty shopping plan.");
        }
        catch (JsonException exception)
        {
            throw new InvalidOperationException("AI returned invalid shopping-plan JSON.", exception);
        }
    }

    private static List<ShoppingLineDto> NormalizeShoppingList(
        List<ShoppingLineDto>? lines)
    {
        if (lines is null)
        {
            throw new InvalidOperationException("AI response is missing the shopping list.");
        }

        if (lines.Count > MaximumShoppingLines)
        {
            throw new InvalidOperationException($"AI response contains more than {MaximumShoppingLines} shopping items.");
        }

        var normalized = new List<ShoppingLineDto>(lines.Count);
        var uniqueLines = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var line in lines)
        {
            if (line is null)
            {
                throw new InvalidOperationException("AI response contains an empty shopping item.");
            }

            var name = NormalizeRequiredText(line.Name, MaximumShoppingNameCharacters, "Shopping-item name");

            var unit = NormalizeRequiredText(line.Unit, MaximumUnitCharacters, "Shopping-item unit");

            if (line.Quantity <= 0 || line.Quantity > MaximumShoppingQuantity)
            {
                throw new InvalidOperationException($"AI response shopping quantity must be greater than zero and no more than {MaximumShoppingQuantity:N0}.");
            }

            if (unit.Any(character => !char.IsLetter(character) && !char.IsWhiteSpace(character) && character is not '/' and not '.'))
            {
                throw new InvalidOperationException("AI response contains an unsupported shopping-item unit.");
            }

            var uniquenessKey = $"{name}\u001f{unit}";

            if (!uniqueLines.Add(uniquenessKey))
            {
                throw new InvalidOperationException("AI response contains a duplicate shopping item.");
            }

            normalized.Add(new ShoppingLineDto
            {
                Name = name,
                Quantity = line.Quantity,
                Unit = unit.ToLowerInvariant()
            });
        }

        return normalized;
    }

    private static List<WeeklyMealDto> NormalizeMealDays(List<WeeklyMealDto>? meals, IReadOnlyList<DailyTargetDto> expectedDays)
    {
        if (meals is null)
        {
            throw new InvalidOperationException("AI response is missing the meal-plan days.");
        }

        if (meals.Count != expectedDays.Count)
        {
            throw new InvalidOperationException("AI response has a missing or unexpected meal-plan day.");
        }

        var returnedByDay = new Dictionary<string, WeeklyMealDto>(StringComparer.OrdinalIgnoreCase);

        foreach (var meal in meals)
        {
            if (meal is null)
            {
                throw new InvalidOperationException("AI response contains an empty meal-plan day.");
            }

            var day = NormalizeRequiredText(meal.Day, 20, "Meal-plan day");

            if (!returnedByDay.TryAdd(day, meal))
            {
                throw new InvalidOperationException("AI response contains a duplicate meal-plan day.");
            }
        }

        var expectedDayNames = expectedDays.Select(day => day.Day).ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (returnedByDay.Keys.Any(day => !expectedDayNames.Contains(day)))
        {
            throw new InvalidOperationException("AI response contains an unexpected meal-plan day.");
        }

        var normalized = new List<WeeklyMealDto>(expectedDays.Count);

        foreach (var expectedDay in expectedDays)
        {
            if (!returnedByDay.TryGetValue(expectedDay.Day, out var meal))
            {
                throw new InvalidOperationException("AI response is missing a required meal-plan day.");
            }

            ValidateRange(
                meal.Calories,
                MinimumDailyCalories,
                MaximumDailyCalories,
                "Daily calories");

            ValidateRange(
                meal.ProteinGrams,
                0,
                MaximumMacroGrams,
                "Daily protein");

            ValidateRange(
                meal.CarbsGrams,
                0,
                MaximumMacroGrams,
                "Daily carbohydrates");

            ValidateRange(
                meal.FatGrams,
                0,
                MaximumMacroGrams,
                "Daily fat");

            meal.Day = expectedDay.Day;
            meal.MealDateLocal = expectedDay.DateLocal.Date;
            meal.IsGymDay = expectedDay.IsGymDay;
            meal.Title = NormalizeRequiredText(meal.Title, MaximumTitleCharacters, "Meal-plan title");

            meal.MealDetails = NormalizeMealDetails(meal.MealDetails);

            normalized.Add(meal);
        }

        return normalized;
    }

    private static DailyMealDetailsDto NormalizeMealDetails(DailyMealDetailsDto? details)
    {
        if (details is null)
        {
            throw new InvalidOperationException("AI response is missing meal details.");
        }

        details.Breakfast = NormalizeMealDetail(
            details.Breakfast,
            "Breakfast",
            requireContent: true);

        details.Lunch = NormalizeMealDetail(
            details.Lunch,
            "Lunch",
            requireContent: true);

        details.Dinner = NormalizeMealDetail(
            details.Dinner,
            "Dinner",
            requireContent: true);

        details.Snack = NormalizeMealDetail(
            details.Snack,
            "Snack",
            requireContent: false);

        details.LateSnack = NormalizeMealDetail(
            details.LateSnack,
            "Late snack",
            requireContent: false);

        return details;
    }

    private static MealDetailDto NormalizeMealDetail(MealDetailDto? detail, string fieldName, bool requireContent)
    {
        if (detail is null)
        {
            throw new InvalidOperationException($"AI response is missing {fieldName.ToLowerInvariant()} details.");
        }

        detail.Overview = NormalizeOptionalText(
            detail.Overview,
            MaximumOverviewCharacters,
            $"{fieldName} overview");

        detail.Ingredients = NormalizeTextList(
            detail.Ingredients,
            MaximumIngredientsPerMeal,
            MaximumIngredientCharacters,
            $"{fieldName} ingredients");

        detail.Steps = NormalizeTextList(
            detail.Steps,
            MaximumStepsPerMeal,
            MaximumStepCharacters,
            $"{fieldName} steps");

        if (requireContent && (string.IsNullOrWhiteSpace(detail.Overview) || detail.Ingredients.Count == 0 || detail.Steps.Count == 0))
        {
            throw new InvalidOperationException($"AI response contains incomplete {fieldName.ToLowerInvariant()} details.");
        }

        return detail;
    }

    private static List<string> NormalizeTextList(List<string>? values,int maximumItems, int maximumCharacters, string fieldName)
    {
        if (values is null)
        {
            throw new InvalidOperationException($"AI response is missing {fieldName.ToLowerInvariant()}.");
        }

        if (values.Count > maximumItems)
        {
            throw new InvalidOperationException($"AI response contains too many {fieldName.ToLowerInvariant()}.");
        }

        return values
            .Select(value => NormalizeRequiredText(
                value,
                maximumCharacters,
                fieldName))
            .ToList();
    }

    private static string NormalizeRequiredText(string? value, int maximumCharacters, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException($"AI response is missing {fieldName.ToLowerInvariant()}.");
        }

        return NormalizeOptionalText(value, maximumCharacters, fieldName);
    }

    private static string NormalizeOptionalText(string? value, int maximumCharacters, string fieldName)
    {
        var normalized = value?.Trim() ?? string.Empty;

        if (normalized.Length > maximumCharacters)
        {
            throw new InvalidOperationException($"AI response {fieldName.ToLowerInvariant()} is too long.");
        }

        return normalized;
    }

    private static void ValidateRange(int value, int minimum, int maximum, string fieldName)
    {
        if (value < minimum || value > maximum)
        {
            throw new InvalidOperationException($"AI response {fieldName.ToLowerInvariant()} must be between {minimum} and {maximum}.");
        }
    }
}