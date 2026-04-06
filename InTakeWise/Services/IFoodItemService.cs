using InTakeWise.Models;

namespace InTakeWise.Services
{
    public interface IFoodItemService
    {
        Task<List<UserFoodItemDto>> GetFoodItemsAsync(string userId);
    }
}