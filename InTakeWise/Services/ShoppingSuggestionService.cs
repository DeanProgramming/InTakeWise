using InTakeWise.Models;
using InTakeWise.Dto;

namespace InTakeWise.Services
{
    public class ShoppingSuggestionService : IShoppingSuggestionService
    {
        public Task<List<ShoppingLineDto>> GetShoppingListAsync(string userId, List<FoodItem> currentInHouse)
        {
            var foodItems = new List<ShoppingLineDto>
            {
                new ShoppingLineDto
                {
                    Name = "CHICKEN",
                    Quantity = 1,
                    Unit = "kg"
                }
            };

            return Task.FromResult(foodItems);
        }

        public Task<List<WeeklyMealDto>> GetWeekSummaryAsync(
            string userId,
            List<FoodItem> currentInHouse,
            List<ShoppingLineDto> itemsWithShopping)
        {
            var mealsPotential = new List<WeeklyMealDto>
            {
                new WeeklyMealDto
                {
                    Day = "Monday",
                    Title = "B: Eggs on toast, D: Chicken and Rice, T: Tuna Salad",
                    Calories = 500,
                    ProteinGrams = 25,
                    CarbsGrams = 150,
                    FatGrams = 10
                }
            };

            return Task.FromResult(mealsPotential);
        }
    }
}
