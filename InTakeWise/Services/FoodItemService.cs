using InTakeWise.Models;

namespace InTakeWise.Services
{
    public class FoodItemService : IFoodItemService
    {
        public Task<List<FoodItem>> GetFoodItemsAsync(string userId)
        {
            var items = new List<FoodItem>
            {
                new() { Id = 1, Name = "Chicken breast", CaloriesPer100g = 165, ProteinPer100g = 31 },
                new() { Id = 2, Name = "Rice" },
                new() { Id = 3, Name = "Broccoli" },
                new() { Id = 4, Name = "Greek yogurt", CaloriesPer100g = 59, ProteinPer100g = 10 }
            };

            return Task.FromResult(items);
        }
    }
}
