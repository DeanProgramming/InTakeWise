using InTakeWise.Models;
using InTakeWise.Services;
using InTakeWise.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace InTakeWise.Controllers
{
    [Authorize]
    public class MealSuggestionController : Controller
    {
        private readonly IFoodItemService _foodItemService;
        private readonly IMealSuggestionService _mealService;

        public MealSuggestionController(IFoodItemService foodService, IMealSuggestionService mealService)
        {
            _foodItemService = foodService;
            _mealService = mealService;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId))
                return Unauthorized();

            var foods = await _foodItemService.GetFoodItemsAsync(userId);
            var suggestedMeal = await _mealService.GetSuggestedMealAsync(userId, foods);

            var vm = new MealSuggestionViewModel
            {
                SuggestedMeal = suggestedMeal,
                StatusMessage = "Suggested meal generated from your fridge items."
            };

            return View("MealSuggestion", vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Regenerate()
        { 
            return await Index();
        }
    }
}
