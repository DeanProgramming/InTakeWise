using InTakeWise.Data;
using InTakeWise.Helper;
using InTakeWise.Models;
using InTakeWise.Services;
using InTakeWise.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace InTakeWise.Controllers;

[Authorize]
public class LogEntryController : Controller
{
    private readonly ILogger<LogEntryController> _logger;
    private readonly UserManager<IdentityUser> _userManager;
    private readonly ILogEntryService _logEntryService;

    public LogEntryController(ILogger<LogEntryController> logger, UserManager<IdentityUser> userManager, ILogEntryService logEntryService)
    {
        _logger = logger;
        _userManager = userManager;
        _logEntryService = logEntryService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(LogType.LoggingType mode, MealType meal = MealType.Breakfast)
    {
        var user = await _userManager.GetUserAsync(User);

        if (user is null)
        {
            return Unauthorized();
        }

        if (!Enum.IsDefined(mode))
        {
            mode = LogType.LoggingType.Meal;
        }

        if (!Enum.IsDefined(meal))
        {
            meal = MealType.Breakfast;
        }

        var vm = await BuildViewModelAsync(
            user,
            mode,
            meal,
            userInput: null,
            error: null);

        if (mode == LogType.LoggingType.Workout && TempData["LastWorkoutLogId"] is string workoutIdValue && int.TryParse(workoutIdValue, out var workoutId))
        {
            vm.GeneratedWorkoutLog =
                await _logEntryService.GetWorkoutByIdAsync(
                    workoutId,
                    user.Id);

            vm.UserInput =
                vm.GeneratedWorkoutLog?.RawInput ?? string.Empty;
        }

        return View("Log", vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(32 * 1024)]
    public async Task<IActionResult> Meal(string? userInput, MealType logTime, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.GetUserAsync(User);

        if (user is null)
        {
            return Unauthorized();
        }

        if (!ModelState.IsValid || !Enum.IsDefined(logTime))
        {
            return View(
                "Log",
                await BuildViewModelAsync(
                    user,
                    LogType.LoggingType.Meal,
                    MealType.Breakfast,
                    userInput,
                    "Select a valid meal time."));
        }

        if (IsDemoUser())
        {
            _logger.LogWarning(
                "Blocked demo meal-analysis request.");

            return RedirectToAction(
                nameof(Index),
                new
                {
                    mode = LogType.LoggingType.Meal,
                    meal = logTime
                });
        }

        try
        {
            var normalizedInput = AiInputValidator.NormalizeLogInput(
                userInput,
                AiOperation.MealAnalysis);

            await _logEntryService.LogMealInfoAsync(
                user.Id,
                normalizedInput,
                logTime,
                cancellationToken);

            return RedirectToAction(
                nameof(Index),
                new
                {
                    mode = LogType.LoggingType.Meal,
                    meal = logTime
                });
        }
        catch (OperationCanceledException)
            when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (AiOperationException exception)
        {
            ApplyFailureStatus(exception);

            LogHandledFailure(
                user.Id,
                AiOperation.MealAnalysis,
                exception);

            return View(
                "Log",
                await BuildViewModelAsync(
                    user,
                    LogType.LoggingType.Meal,
                    Enum.IsDefined(logTime)
                        ? logTime
                        : MealType.Breakfast,
                    userInput,
                    exception.UserMessage));
        }
        catch (Exception exception)
        {
            LogUnexpectedFailure(
                user.Id,
                AiOperation.MealAnalysis,
                exception);

            return View(
                "Log",
                await BuildViewModelAsync(
                    user,
                    LogType.LoggingType.Meal,
                    Enum.IsDefined(logTime)
                        ? logTime
                        : MealType.Breakfast,
                    userInput,
                    "Meal analysis is temporarily unavailable. Please try again."));
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(32 * 1024)]
    public async Task<IActionResult> Workout(string? userInput, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.GetUserAsync(User);

        if (user is null)
        {
            return Unauthorized();
        }

        if (IsDemoUser())
        {
            _logger.LogWarning(
                "Blocked demo workout-analysis request.");

            return RedirectToAction(
                nameof(Index),
                new
                {
                    mode = LogType.LoggingType.Workout
                });
        }

        try
        {
            var normalizedInput = AiInputValidator.NormalizeLogInput(
                userInput,
                AiOperation.WorkoutAnalysis);

            var log = await _logEntryService.LogWorkoutInfoAsync(
                user.Id,
                normalizedInput,
                cancellationToken);

            TempData["LastWorkoutLogId"] = log.Id.ToString();

            return RedirectToAction(
                nameof(Index),
                new
                {
                    mode = LogType.LoggingType.Workout
                });
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (AiOperationException exception)
        {
            ApplyFailureStatus(exception);

            LogHandledFailure(
                user.Id,
                AiOperation.WorkoutAnalysis,
                exception);

            return View("Log",
                await BuildViewModelAsync(
                    user,
                    LogType.LoggingType.Workout,
                    MealType.Breakfast,
                    userInput,
                    exception.UserMessage));
        }
        catch (Exception exception)
        {
            LogUnexpectedFailure(
                user.Id,
                AiOperation.WorkoutAnalysis,
                exception);

            return View("Log",
                await BuildViewModelAsync(
                    user,
                    LogType.LoggingType.Workout,
                    MealType.Breakfast,
                    userInput,
                    "Workout analysis is temporarily unavailable. Please try again."));
        }
    }

    private async Task<LogEntryViewModel> BuildViewModelAsync(
        IdentityUser user,
        LogType.LoggingType mode,
        MealType meal,
        string? userInput,
        string? error)
    {
        var viewModel = new LogEntryViewModel
        {
            UserName = user.UserName,
            Mode = mode,
            SelectedTimeOfDay = meal,
            UserInput = NormalizeInputForRedisplay(userInput),
            Error = error,
            DailySummary =
                await _logEntryService.GetTodaySummaryAsync(
                    user.Id)
        };

        if (mode == LogType.LoggingType.Meal)
        {
            viewModel.GeneratedMealLog =
                await _logEntryService.GetTodayMealAsync(
                    user.Id,
                    meal);

            if (string.IsNullOrWhiteSpace(userInput))
            {
                viewModel.UserInput =
                    viewModel.GeneratedMealLog?.RawInput
                    ?? string.Empty;
            }
        }
        else if (IsDemoUser() || !string.IsNullOrWhiteSpace(error))
        {
            viewModel.GeneratedWorkoutLog =
                await _logEntryService.GetLatestWorkoutAsync(
                    user.Id);
        }

        return viewModel;
    }

    private static string NormalizeInputForRedisplay(
        string? userInput)
    {
        var normalized = userInput?.Trim() ?? string.Empty;

        return normalized.Length <= AiInputValidator.MaximumLogInputCharacters ? normalized : normalized[..AiInputValidator.MaximumLogInputCharacters];
    }

    private void LogHandledFailure(
        string userId,
        AiOperation operation,
        AiOperationException exception)
    {
        _logger.LogWarning(
            "AI request was handled without exposing provider data. Operation={Operation}; User={UserReference}; FailureType={FailureType}.",
            operation,
            AiLogSanitizer.UserReference(userId),
            exception.GetType().Name);
    }

    private void ApplyFailureStatus(
        AiOperationException exception)
    {
        if (exception is not AiRequestLimitException limitException)
        {
            return;
        }

        Response.StatusCode = StatusCodes.Status429TooManyRequests;
        Response.Headers["Retry-After"] = Math.Max(
                1,
                (int)Math.Ceiling(
                    limitException.RetryAfter.TotalSeconds))
            .ToString();
    }

    private void LogUnexpectedFailure(
        string userId,
        AiOperation operation,
        Exception exception)
    {
        _logger.LogError(
            "Unexpected AI request failure. Operation={Operation}; User={UserReference}; FailureType={FailureType}.",
            operation,
            AiLogSanitizer.UserReference(userId),
            exception.GetType().Name);
    }

    private bool IsDemoUser() => User.HasClaim(DbSeeder.DemoClaimType, DbSeeder.DemoClaimValue);
}
