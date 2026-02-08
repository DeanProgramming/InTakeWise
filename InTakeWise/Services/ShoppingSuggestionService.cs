using InTakeWise.Models;
using InTakeWise.Dto;

namespace InTakeWise.Services
{
    public class ShoppingSuggestionService : IShoppingSuggestionService
    {
        public Task<ShoppingPlanDto> GenerateWeekPlanAsync(string userId, List<FoodItem> currentInHouse)
        {
            var plan = new ShoppingPlanDto
            {
                ShoppingList = new()
        {
            new ShoppingLineDto { Name = "CHICKEN", Quantity = 1, Unit = "kg" }
        },
                WeekMealsSummary = new()
        {
            new WeeklyMealDto
            {
                Day = "Monday",
                Title = "B: Eggs on toast, D: Chicken and Rice, T: Tuna Salad",
                Calories = 500, ProteinGrams = 25, CarbsGrams = 150, FatGrams = 10
            }
        }
            };

            return Task.FromResult(plan);
        } 
    }
}
