using System.Net;
using System.Security.Claims;
using InTakeWise.Services;
using InTakeWise.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InTakeWise.Controllers
{
    [Authorize]
    public class ShoppingSuggestionController : Controller
    {
        private readonly IFoodItemService _foodItemService;
        private readonly IShoppingSuggestionService _shoppingSuggestionService;
        private readonly ILogger<ShoppingSuggestionController> _logger;

        public ShoppingSuggestionController(
            IFoodItemService foodItemService,
            IShoppingSuggestionService shoppingSuggestionService,
            ILogger<ShoppingSuggestionController> logger)
        {
            _foodItemService = foodItemService;
            _shoppingSuggestionService = shoppingSuggestionService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId))
                return Unauthorized();

            var savedPlan = await _shoppingSuggestionService.GetSavedWeekPlanAsync(userId);

            var vm = new ShoppingSuggestionViewModel
            {
                ShoppingList = savedPlan?.ShoppingList ?? new(),
                WeekMealsSummary = savedPlan?.WeekMealsSummary ?? new()
            };

            return View("ShoppingSuggestion", vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GetShoppingList()
        {
            var vm = new ShoppingSuggestionViewModel();

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId))
                return Unauthorized();

            try
            {
                var pantryItems = await _foodItemService.GetFoodItemsAsync(userId);
                var plan = await _shoppingSuggestionService.GenerateWeekPlanAsync(userId, pantryItems);

                await _shoppingSuggestionService.SaveWeekPlanAsync(userId, plan);

                return RedirectToAction(nameof(Index));
            }
            catch (TaskCanceledException ex)
            {
                _logger.LogError(ex, "OpenAI call timed out while generating shopping plan.");
                vm.Error = "The request timed out. Please try again.";
                return View("ShoppingSuggestion", vm);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Shopping plan generation could not be completed.");
                vm.Error = ex.Message;
                return View("ShoppingSuggestion", vm);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "OpenAI call failed while generating shopping plan.");

                var statusCode = TryGetStatusCode(ex);

                vm.Error = statusCode switch
                {
                    401 => "Authentication failed (invalid or revoked API key). Check OPENAI_API_KEY and try again.",
                    403 => "Permission denied. Your key or project may not have access, or your region/IP may be restricted.",
                    429 => "Rate limit or quota exceeded. Check your billing and usage limits, then try again.",
                    500 or 503 => "OpenAI service is having trouble. Please retry in a moment.",
                    _ => "Something went wrong while generating the shopping plan. Check logs for details."
                };

                return View("ShoppingSuggestion", vm);
            }
        }

        private static int? TryGetStatusCode(Exception ex)
        {
            var t = ex.GetType();
            var prop =
                t.GetProperty("StatusCode") ??
                t.GetProperty("Status") ??
                t.GetProperty("HttpStatusCode");

            if (prop == null) return null;

            var val = prop.GetValue(ex);
            return val switch
            {
                int i => i,
                HttpStatusCode code => (int)code,
                _ => null
            };
        }
    }
}