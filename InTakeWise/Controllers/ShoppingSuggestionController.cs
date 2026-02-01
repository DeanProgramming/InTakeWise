using InTakeWise.Models;
using InTakeWise.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace InTakeWise.Controllers
{
    public class ShoppingSuggestionController : Controller
    {
        private readonly IFoodItemService _foodItemService;
        private readonly IShoppingSuggestionService _shoppingSuggestionService;

        public ShoppingSuggestionController(IFoodItemService foodService, IShoppingSuggestionService shoppingSuggestionService)
        {
            _foodItemService = foodService;
            _shoppingSuggestionService = shoppingSuggestionService;
        }

        [Authorize]
        public async Task<IActionResult> Index()
        { 
            var vm = new HomeViewModel
            {
                UserName = User?.Identity?.Name ?? "Demo User", 
            };

            return View("ShoppingSuggestion", vm); 
        } 

        [HttpPost]
        public async Task<IActionResult> GetShoppingList()
        { 
            var userId = User?.Identity?.Name ?? null;
            if (userId == null) return Unauthorized();

            var foodService = await _foodItemService.GetFoodItemsAsync(userId);
            var suggestedShoppingList = await _shoppingSuggestionService.GetShoppingListAsync(userId, foodService);
            var suggestedWeekMeals = await _shoppingSuggestionService.GetWeekSummaryAsync(userId, foodService, suggestedShoppingList);



            var vm = new HomeViewModel
            {
                UserName = User?.Identity?.Name ?? "Demo User",
                ShoppingSuggestionPlan =
                {
                    ShoppingList = suggestedShoppingList,
                    WeekMealsSummary = suggestedWeekMeals
                }
            };

            return View("ShoppingSuggestion", vm); 
        }

    }
}