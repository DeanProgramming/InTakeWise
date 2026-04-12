using InTakeWise.Dto;
using InTakeWise.Models;

namespace InTakeWise.Services
{
    public interface IShoppingSuggestionService
    {
        Task<ShoppingPlanDto> GenerateWeekPlanAsync(string userId, List<UserFoodItemDto> currentInHouse);
        Task SaveWeekPlanAsync(string userId, ShoppingPlanDto plan);
        Task<ShoppingPlanDto?> GetSavedWeekPlanAsync(string userId);
    }
}