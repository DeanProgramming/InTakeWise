using InTakeWise.Models;
using InTakeWise.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace InTakeWise.Controllers
{
    public class MealSuggestionController : Controller
    {
        private readonly IFoodItemService _foodItemService;
        private readonly IMealSuggestionService _mealService;

        public MealSuggestionController(IFoodItemService foodService, IMealSuggestionService mealService)
        {
            _foodItemService = foodService;
            _mealService = mealService;
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Index()
        { 
            var userId = User?.Identity?.Name ?? "demo-user";

            var foodService = await _foodItemService.GetFoodItemsAsync(userId);
            var suggestedMeal = await _mealService.GetSuggestedMealAsync(userId, foodService);

            var vm = new HomeViewModel
            {
                UserName = User?.Identity?.Name ?? "Demo User",
                MealPlan =
                {
                    ItemsAtHome = foodService,
                    SuggestedMeal = suggestedMeal,
                    StatusMessage = "Suggested meal generated from your fridge items."
                }
            };

            return View("MealSuggestion", vm);
        }
    }
}