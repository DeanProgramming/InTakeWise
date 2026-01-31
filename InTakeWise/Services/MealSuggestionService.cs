using InTakeWise.Models;
using InTakeWise.Services;

namespace InTakeWise.Services
{
    public class DummyMealSuggestionService : IMealSuggestionService
    {
        public Task<MealSuggestion> GetSuggestedMealAsync(string userId, List<FoodItem> items)
        {
            // Later: generate meal using items + user macro targets + restrictions.
            // Today: return a static suggestion.
            var meal = new MealSuggestion
            {
                Title = "Chicken, Rice & Broccoli Bowl",
                Description = "A simple high-protein bowl using what you have.",
                Calories = 620,
                ProteinGrams = 55,
                CarbsGrams = 70,
                FatGrams = 12,
                IngredientsUsed = new List<string>
            {
                "Chicken breast (250g)",
                "Rice (150g cooked portion)",
                "Broccoli (1 cup)",
                "Salt, pepper, garlic (optional)"
            },
                Steps = new List<string>
            {
                "Season and pan-cook chicken until done.",
                "Cook rice (or reheat if already cooked).",
                "Steam broccoli for 4–6 minutes.",
                "Assemble bowl and serve."
            }
            };

            return Task.FromResult(meal);
        }
    }

}

