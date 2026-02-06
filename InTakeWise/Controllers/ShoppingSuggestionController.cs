using InTakeWise.Dto;
using InTakeWise.Services;
using InTakeWise.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace InTakeWise.Controllers
{
    [Authorize]
    public class ShoppingSuggestionController : Controller
    {
        private readonly IFoodItemService _foodItemService;
        private readonly IShoppingSuggestionService _shoppingSuggestionService;

        public ShoppingSuggestionController(
            IFoodItemService foodService,
            IShoppingSuggestionService shoppingSuggestionService)
        {
            _foodItemService = foodService;
            _shoppingSuggestionService = shoppingSuggestionService;
        }

        public IActionResult Index()
        {
            return View("ShoppingSuggestion", new ShoppingSuggestionViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GetShoppingList()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId))
                return Unauthorized();

            var foods = await _foodItemService.GetFoodItemsAsync(userId);

            var shoppingList = await _shoppingSuggestionService.GetShoppingListAsync(userId, foods);
            var weekMeals = await _shoppingSuggestionService.GetWeekSummaryAsync(userId, foods, shoppingList);

            var vm = new ShoppingSuggestionViewModel
            {
                ShoppingList = shoppingList.Select(x => new ShoppingLineVm
                {
                    Name = x.Name,
                    Quantity = x.Quantity,
                    Unit = x.Unit
                }).ToList(),

                WeekMealsSummary = weekMeals.Select(x => new WeeklyMealVm
                {
                    Day = x.Day,
                    Title = x.Title,
                    Calories = x.Calories,
                    ProteinGrams = x.ProteinGrams,
                    CarbsGrams = x.CarbsGrams,
                    FatGrams = x.FatGrams
                }).ToList()
            };

            return View("ShoppingSuggestion", vm);
        }
    }
}
