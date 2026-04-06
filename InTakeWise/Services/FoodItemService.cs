using InTakeWise.Data;
using InTakeWise.Models;
using Microsoft.EntityFrameworkCore;

namespace InTakeWise.Services
{
    public class FoodItemService : IFoodItemService
    {
        private readonly ApplicationDbContext _db;

        public FoodItemService(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<List<UserFoodItemDto>> GetFoodItemsAsync(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
                return new List<UserFoodItemDto>();

            return await _db.PantryItems
                .AsNoTracking()
                .Where(x => x.UserId == userId)
                .OrderBy(x => x.FoodItem.Name)
                .Select(x => new UserFoodItemDto
                {
                    PantryItemId = x.Id,
                    FoodItemId = x.FoodItemId,
                    Name = x.FoodItem.Name,
                    Quantity = x.Quantity,
                    Unit = x.Unit ?? "",
                    CaloriesPer100g = x.FoodItem.CaloriesPer100g,
                    ProteinPer100g = x.FoodItem.ProteinPer100g,
                    CarbsPer100g = x.FoodItem.CarbsPer100g,
                    FatPer100g = x.FoodItem.FatPer100g,
                    FiberPer100g = x.FoodItem.FiberPer100g
                })
                .ToListAsync();
        }
    }
}