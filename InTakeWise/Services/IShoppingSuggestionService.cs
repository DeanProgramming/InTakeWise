
using InTakeWise.Models;

namespace InTakeWise.Services
{
    public interface IShoppingSuggestionService
    {
        Task<List<FoodItem>> GetShoppingListAsync(string userId, List<FoodItem> items);
        Task<List<ShoppingMealsWeekSuggestion>> GetWeekSummaryAsync(string userId, List<FoodItem> items, List<FoodItem> weeksShop);
    }

}
