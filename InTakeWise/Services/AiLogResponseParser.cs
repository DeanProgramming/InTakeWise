using System.Text.Json;
using System.Text.Json.Serialization;
using InTakeWise.Dto;

namespace InTakeWise.Services;

public interface IAiLogResponseParser
{
    MealAnalysisDto ParseMeal(string? json);
    WorkoutAnalysisDto ParseWorkout(string? json);
}

public sealed class AiLogResponseParser : IAiLogResponseParser
{
    private const int MaximumMealCalories = 10_000;
    private const int MaximumMacroGrams = 1_000;
    private const int MaximumFiberGrams = 250;
    private const int MaximumWorkoutMinutes = 480;
    private const int MaximumWorkoutCalories = 5_000;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow
    };

    public MealAnalysisDto ParseMeal(string? json)
    {
        var response = Deserialize<MealResponse>(json, "meal");

        EnsureRequiredText(response.Summary, "Meal summary");
        EnsureRequired(response.Calories, "Calories");
        EnsureRequired(response.Protein, "Protein");
        EnsureRequired(response.Carbs, "Carbs");
        EnsureRequired(response.Fat, "Fat");
        EnsureRequired(response.Fiber, "Fiber");

        EnsureInRange(response.Calories.Value, 0, MaximumMealCalories, "Calories");
        EnsureInRange(response.Protein.Value, 0, MaximumMacroGrams, "Protein");
        EnsureInRange(response.Carbs.Value, 0, MaximumMacroGrams, "Carbs");
        EnsureInRange(response.Fat.Value, 0, MaximumMacroGrams, "Fat");
        EnsureInRange(response.Fiber.Value, 0, MaximumFiberGrams, "Fiber");

        return new MealAnalysisDto
        {
            Summary = response.Summary!.Trim(),
            Calories = response.Calories.Value,
            Protein = response.Protein.Value,
            Carbs = response.Carbs.Value,
            Fat = response.Fat.Value,
            Fiber = response.Fiber.Value
        };
    }

    public WorkoutAnalysisDto ParseWorkout(string? json)
    {
        var response = Deserialize<WorkoutResponse>(json, "workout");

        EnsureRequiredText(response.ActivityType, "Activity type");
        EnsureRequiredText(response.Intensity, "Intensity");
        EnsureRequired(response.DurationMinutes, "Duration minutes");
        EnsureRequired(response.CaloriesBurned, "Calories burned");

        EnsureInRange(response.DurationMinutes.Value, 1, MaximumWorkoutMinutes, "Duration minutes");
        EnsureInRange(response.CaloriesBurned.Value, 0, MaximumWorkoutCalories, "Calories burned");

        return new WorkoutAnalysisDto
        {
            ActivityType = response.ActivityType!.Trim(),
            DurationMinutes = response.DurationMinutes.Value,
            Intensity = response.Intensity!.Trim(),
            CaloriesBurned = response.CaloriesBurned.Value
        };
    }

    private static T Deserialize<T>(string? json, string responseName)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            throw new InvalidOperationException($"AI returned an empty {responseName} JSON response.");
        }

        try
        {
            return JsonSerializer.Deserialize<T>(json, JsonOptions)
                ?? throw new InvalidOperationException($"AI returned an empty {responseName} analysis.");
        }
        catch (JsonException exception)
        {
            throw new InvalidOperationException($"AI returned invalid {responseName} JSON.", exception);
        }
    }

    private static void EnsureRequired(int? value, string name)
    {
        if (!value.HasValue)
        {
            throw new InvalidOperationException($"AI response is missing {name}.");
        }
    }

    private static void EnsureRequiredText(string? value, string name)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException($"AI response is missing {name}.");
        }
    }

    private static void EnsureInRange(int value, int minimum, int maximum, string name)
    {
        if (value < minimum || value > maximum)
        {
            throw new InvalidOperationException(
                $"AI response {name} must be between {minimum} and {maximum}.");
        }
    }

    private sealed class MealResponse
    {
        public MealResponse()
        {
        }

        public string? Summary { get; set; }
        public int? Calories { get; set; }
        public int? Protein { get; set; }
        public int? Carbs { get; set; }
        public int? Fat { get; set; }
        public int? Fiber { get; set; }
    }

    private sealed class WorkoutResponse
    {
        public WorkoutResponse()
        {
        }

        public string? ActivityType { get; set; }
        public int? DurationMinutes { get; set; }
        public string? Intensity { get; set; }
        public int? CaloriesBurned { get; set; }
    }
}
