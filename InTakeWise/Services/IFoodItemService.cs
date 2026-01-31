
using InTakeWise.Models;

namespace InTakeWise.Services
{
    public interface IFoodItemService
    {
        Task<List<FoodItem>> GetFoodItemsAsync(string userId);
    }

}
