using InTakeWise.Models;

namespace InTakeWise.Services
{
    public class DummyMealSuggestionService : IMealSuggestionService
    {
        public Task<TodayMealPlan> GetTodayPlanAsync(string userId, List<UserFoodItemDto> items)
        {
            var plan = new TodayMealPlan
            {
                Breakfast = new MealSuggestion
                {
                    Title = "Eggs on Toast",
                    Description = "Quick protein breakfast using fridge basics.",
                    Calories = 450,
                    ProteinGrams = 25,
                    CarbsGrams = 45,
                    FatGrams = 18,
                    IngredientsUsed = new() { "Eggs (3)", "Bread (2 slices)", "Butter (optional)" },
                    Steps = new() { "Toast bread.", "Scramble eggs.", "Serve together." }
                },
                Dinner = new MealSuggestion
                {
                    Title = "Chicken, Rice & Broccoli",
                    Description = "High-protein dinner bowl.",
                    Calories = 650,
                    ProteinGrams = 55,
                    CarbsGrams = 70,
                    FatGrams = 12,
                    IngredientsUsed = new() { "Chicken breast (250g)", "Rice (150g cooked)", "Broccoli (1 cup)" },
                    Steps = new() { "Cook chicken.", "Cook rice.", "Steam broccoli.", "Assemble." }
                },
                Tea = new MealSuggestion
                {
                    Title = "Tuna Salad",
                    Description = "Light evening meal.",
                    Calories = 380,
                    ProteinGrams = 35,
                    CarbsGrams = 12,
                    FatGrams = 18,
                    IngredientsUsed = new() { "Tuna (1 can)", "Mixed leaves", "Olive oil / dressing" },
                    Steps = new() { "Drain tuna.", "Mix with leaves.", "Add dressing." }
                }
            };

            return Task.FromResult(plan);
        }
    }
}
