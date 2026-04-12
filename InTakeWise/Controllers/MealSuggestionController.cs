using System.Security.Claims;
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
        public async Task<IActionResult> Index(string meal = "Breakfast")
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId))
                return Unauthorized();

            var selectedMeal = NormalizeMeal(meal);
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
                    plan.Breakfast != null ? "Breakfast" :
                    plan.Lunch != null ? "Lunch" :
                    plan.Dinner != null ? "Dinner" :
                    "Breakfast";
            }

            return View("MealSuggestion", vm);
        }
        private static string NormalizeMeal(string? meal)
        {
            return meal?.Trim().ToLowerInvariant() switch
            {
                "breakfast" => "Breakfast",
                "lunch" => "Lunch",
                "dinner" => "Dinner",
                "snack" => "Snack",
                _ => "Breakfast"
            };
        }
    }
}