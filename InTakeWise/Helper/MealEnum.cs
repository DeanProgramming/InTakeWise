namespace InTakeWise.Helper
{
    public enum MealType
    {
        Breakfast,
        Dinner,
        Tea,
        Snack
    }

    public static class MealTypeExtensions
    {
        public static string ToDisplayName(this MealType meal) => meal switch
        {
            MealType.Breakfast => "Breakfast",
            MealType.Dinner => "Dinner",
            MealType.Tea => "Tea",
            MealType.Snack => "Snack",
            _ => meal.ToString()
        };

        public static MealType ParseOrDefault(string? value) =>
            value?.Trim().ToLowerInvariant() switch
            {
                "breakfast" => MealType.Breakfast,
                "dinner" => MealType.Dinner,
                "lunch" => MealType.Dinner,         
                "tea" => MealType.Tea,
                "evening" => MealType.Tea,          
                "snack" => MealType.Snack,
                "late snack" => MealType.Snack,     
                "latesnack" => MealType.Snack,
                _ => MealType.Breakfast
            };
    }
}