namespace InTakeWise.Services;

public static class AiInputValidator
{
    public const int MaximumLogInputCharacters = 2_000;
    public const int MaximumPantryItemsInPrompt = 150;
    public const int MaximumPantryNameCharacters = 100;
    public const int MaximumPantryUnitCharacters = 30;

    public static string NormalizeLogInput(string? userInput, AiOperation operation)
    {
        if (operation is not (AiOperation.MealAnalysis or AiOperation.WorkoutAnalysis))
        {
            throw new ArgumentOutOfRangeException(nameof(operation));
        }

        var displayName = operation == AiOperation.MealAnalysis ? "Meal" : "Workout";

        if (string.IsNullOrWhiteSpace(userInput))
        {
            throw new AiInputValidationException($"{displayName} input cannot be empty.");
        }

        var normalized = userInput.Trim();

        if (normalized.Length > MaximumLogInputCharacters)
        {
            throw new AiInputValidationException($"{displayName} input must be {MaximumLogInputCharacters:N0} characters or fewer.");
        }

        if (normalized.Contains('\0'))
        {
            throw new AiInputValidationException($"{displayName} input contains an unsupported character.");
        }

        return normalized;
    }
}