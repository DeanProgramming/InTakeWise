using InTakeWise.Models;
using InTakeWise.Services; 

namespace InTakeWise.Services
{
    public class FoodItemService : IFoodItemService
    {
        public Task<List<FoodItem>> GetFoodItemsAsync(string userId)
        {
            // Dummy data for now
            var items = new List<FoodItem>
            {
                new() { Name = "Chicken breast", Quantity = 300, Unit = "g", ExpiryDate = DateTime.UtcNow.AddDays(2) },
                new() { Name = "Rice", Quantity = 200, Unit = "g" },
                new() { Name = "Broccoli", Quantity = 1, Unit = "head", ExpiryDate = DateTime.UtcNow.AddDays(3) },
                new() { Name = "Greek yogurt", Quantity = 170, Unit = "g", ExpiryDate = DateTime.UtcNow.AddDays(5) }
            };

            return Task.FromResult(items);
        }
    }

}

