using InTakeWise.Models;

namespace InTakeWise.Services
{
    public interface IMealSuggestionService
    {
        Task<TodayMealPlan> GetTodayPlanAsync(string userId, List<FoodItem> items);
    }
}
