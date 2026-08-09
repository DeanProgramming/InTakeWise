using InTakeWise.Helper;
using InTakeWise.Models;
using InTakeWise.Services;
using InTakeWise.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using InTakeWise.Data;

namespace InTakeWise.Controllers
{
    [Authorize]
    public class LogEntryController : Controller
    {
        private readonly ILogger<LogEntryController> _logger;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ILogEntryService _logEntryService;

        public LogEntryController(
            ILogger<LogEntryController> logger,
            UserManager<IdentityUser> userManager,
            ILogEntryService logEntryService)
        {
            _logger = logger;
            _userManager = userManager;
            _logEntryService = logEntryService;
        }

        [HttpGet]
        public async Task<IActionResult> Index(LogType.LoggingType mode, MealType meal = MealType.Breakfast)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var vm = new LogEntryViewModel
            {
                UserName = user.UserName,
                Mode = mode,
                SelectedTimeOfDay = meal,
                DailySummary = await _logEntryService.GetTodaySummaryAsync(user.Id)
            };

            if (mode == LogType.LoggingType.Meal)
            {
                vm.GeneratedMealLog = await _logEntryService.GetTodayMealAsync(user.Id, meal);
                vm.UserInput = vm.GeneratedMealLog?.RawInput ?? "";
            }
            else if (mode == LogType.LoggingType.Workout &&
                     TempData["LastWorkoutLogId"] is string workoutIdStr &&
                     int.TryParse(workoutIdStr, out var workoutId))
            {
                vm.GeneratedWorkoutLog = await _logEntryService.GetWorkoutByIdAsync(workoutId, user.Id);
                vm.UserInput = vm.GeneratedWorkoutLog?.RawInput ?? "";
            }

            return View("Log", vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Meal(string userInput, LogType.LoggingType mode, MealType logTime)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            if (IsDemoUser())
            {
                _logger.LogWarning("Blocked demo meal-analysis request.");

                return RedirectToAction(nameof(Index), new { mode = LogType.LoggingType.Meal, meal = logTime });
            }

            if (string.IsNullOrWhiteSpace(userInput))
            {
                return View("Log", new LogEntryViewModel
                {
                    UserName = user.UserName,
                    Mode = mode,
                    SelectedTimeOfDay = logTime,
                    DailySummary = await _logEntryService.GetTodaySummaryAsync(user.Id),
                    GeneratedMealLog = await _logEntryService.GetTodayMealAsync(user.Id, logTime)
                });
            }

            await _logEntryService.LogMealInfoAsync(user.Id, userInput, logTime);

            return RedirectToAction(nameof(Index), new { mode = LogType.LoggingType.Meal, meal = logTime });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Workout(string userInput, LogType.LoggingType mode)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            if (IsDemoUser())
            {
                _logger.LogWarning("Blocked demo workout-analysis request.");

                return RedirectToAction(nameof(Index), new { mode = LogType.LoggingType.Workout});
            }

            if (string.IsNullOrWhiteSpace(userInput))
            {
                return View("Log", new LogEntryViewModel
                {
                    UserName = user.UserName,
                    Mode = mode,
                    DailySummary = await _logEntryService.GetTodaySummaryAsync(user.Id)
                });
            }

            var log = await _logEntryService.LogWorkoutInfoAsync(user.Id, userInput);

            TempData["LastWorkoutLogId"] = log.Id.ToString();
            TempData["LastInput"] = userInput;

            return RedirectToAction(nameof(Index), new { mode = LogType.LoggingType.Workout });
        }

        private bool IsDemoUser()
        {
            return User.HasClaim(
                DbSeeder.DemoClaimType,
                DbSeeder.DemoClaimValue);
        }
    }
}
