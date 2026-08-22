using System.Security.Claims;
using InTakeWise.Data;
using InTakeWise.Services;
using InTakeWise.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InTakeWise.Controllers;

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
    public async Task<IActionResult> Index(CancellationToken cancellationToken = default)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        var savedPlan = await _shoppingSuggestionService.GetSavedWeekPlanAsync(userId, cancellationToken);

        return View("ShoppingSuggestion", 
            new ShoppingSuggestionViewModel
            {
                ShoppingList =
                    savedPlan?.ShoppingList ?? new(),
                WeekMealsSummary =
                    savedPlan?.WeekMealsSummary ?? new()
            });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(16 * 1024)]
    public async Task<IActionResult> GetShoppingList(CancellationToken cancellationToken = default)
    {
        var userId = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        if (IsDemoUser())
        {
            _logger.LogWarning("Blocked demo shopping-plan generation request.");

            return RedirectToAction(nameof(Index));
        }

        try
        {
            var pantryItems = await _foodItemService.GetFoodItemsAsync(userId);
            var plan = await _shoppingSuggestionService.GenerateWeekPlanAsync(userId, pantryItems, cancellationToken);
            await _shoppingSuggestionService.SaveWeekPlanAsync(userId, plan, cancellationToken);

            return RedirectToAction(nameof(Index));
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (AiOperationException exception)
        {
            ApplyFailureStatus(exception);

            _logger.LogWarning(
                "Shopping-plan failure handled safely. User={UserReference}; FailureType={FailureType}.",
                AiLogSanitizer.UserReference(userId),
                exception.GetType().Name);

            return View("ShoppingSuggestion", new ShoppingSuggestionViewModel { Error = exception.UserMessage });
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Unexpected shopping-plan failure. User={UserReference}; FailureType={FailureType}.", exception.GetType().Name);

            return View("ShoppingSuggestion", new ShoppingSuggestionViewModel 
            { 
                Error = "Shopping-plan generation is temporarily unavailable. Please try again." 
            });
        }
    }

    private bool IsDemoUser() => User.HasClaim(DbSeeder.DemoClaimType, DbSeeder.DemoClaimValue);

    private void ApplyFailureStatus(AiOperationException exception)
    {
        if (exception is not AiRequestLimitException limitException)
        {
            return;
        }

        Response.StatusCode = StatusCodes.Status429TooManyRequests;
        Response.Headers["Retry-After"] = Math.Max(1, (int)Math.Ceiling(limitException.RetryAfter.TotalSeconds)).ToString();
    }
}