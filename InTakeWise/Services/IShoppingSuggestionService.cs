
using InTakeWise.Models;
using InTakeWise.Dto;

namespace InTakeWise.Services
{
    public interface IShoppingSuggestionService
    {
        Task<ShoppingPlanDto> GenerateWeekPlanAsync(string userId, List<UserFoodItemDto> currentInHouse);
    }

}
