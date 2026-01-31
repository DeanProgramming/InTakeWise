
using InTakeWise.Models;

namespace InTakeWise.Services
{
    public interface IMealSuggestionService
    {
        Task<MealSuggestion> GetSuggestedMealAsync(string userId, List<FoodItem> items);
    }

}
