
using InTakeWise.Models;
using InTakeWise.Dto;

namespace InTakeWise.Services
{
    public interface IShoppingSuggestionService
    {
        Task<List<ShoppingLineDto>> GetShoppingListAsync(string userId, List<FoodItem> items);
        Task<List<WeeklyMealDto>> GetWeekSummaryAsync(
            string userId,
            List<FoodItem> items,
            List<ShoppingLineDto> weeksShop);
    }

}
