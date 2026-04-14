using System.Security.Claims;
using InTakeWise.Helper;
using InTakeWise.Services;
using InTakeWise.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InTakeWise.Controllers
{
    [Authorize]
    public class MealSuggestionController : Controller
    {
        private readonly IMealSuggestionService _mealSuggestionService;

        public MealSuggestionController(IMealSuggestionService mealSuggestionService)
        {
            _mealSuggestionService = mealSuggestionService;
        }

        [HttpGet]
        public async Task<IActionResult> Index(string? meal = null)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId))
                return Unauthorized();

            var selectedMeal = MealTypeExtensions.ParseOrDefault(meal);
            var plan = await _mealSuggestionService.GetTodayPlanAsync(userId);

            var vm = new MealSuggestionViewModel
            {
                Plan = plan,
                SelectedMeal = selectedMeal,
                StatusMessage = plan == null
                    ? "No saved meal plan for today. Generate your weekly shopping plan first."
                    : ""
            };

            if (plan != null && vm.SelectedMealSuggestion == null)
            {
                vm.SelectedMeal =
                    plan.Breakfast != null ? MealType.Breakfast :
                    plan.Dinner != null ? MealType.Dinner :
                    plan.Tea != null ? MealType.Tea :
                    plan.Snack != null ? MealType.Snack :
                    MealType.Breakfast;
            }

            return View("MealSuggestion", vm);
        }
    }
}