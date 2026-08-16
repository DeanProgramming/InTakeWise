using InTakeWise.Dto;
using InTakeWise.Models;

namespace InTakeWise.Services
{
    public interface IShoppingSuggestionService
    {
        Task<ShoppingPlanDto> GenerateWeekPlanAsync(string userId, List<UserFoodItemDto> currentInHouse, CancellationToken cancellationToken = default);

        Task SaveWeekPlanAsync(string userId, ShoppingPlanDto plan, CancellationToken cancellationToken = default);

        Task<ShoppingPlanDto?> GetSavedWeekPlanAsync(string userId, CancellationToken cancellationToken = default);
    }
}
