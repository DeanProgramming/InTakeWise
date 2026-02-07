using InTakeWise.Models;
using InTakeWise.Services;
using InTakeWise.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

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
        public async Task<IActionResult> Index(LogType.LoggingType mode)
        {
            var user = await _userManager.GetUserAsync(User);

            var vm = new LogEntryViewModel
            {
                UserName = user?.UserName,
                Mode = mode,
                UserInput = TempData["LastInput"]?.ToString()
            };

            if (user != null)
            {
                if (mode == LogType.LoggingType.Meal &&
                    TempData["LastMealLogId"] is string mealIdStr &&
                    int.TryParse(mealIdStr, out var mealId))
                {
                    vm.GeneratedMealLog = await _logEntryService.GetMealByIdAsync(mealId, user.Id);
                }
                else if (mode == LogType.LoggingType.Workout &&
                         TempData["LastWorkoutLogId"] is string workoutIdStr &&
                         int.TryParse(workoutIdStr, out var workoutId))
                {
                    vm.GeneratedWorkoutLog = await _logEntryService.GetWorkoutByIdAsync(workoutId, user.Id);
                }
            }

            return View("Log", vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Meal(string userInput, LogType.LoggingType mode)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            if (string.IsNullOrWhiteSpace(userInput))
                return View("Log", new LogEntryViewModel { UserName = user.UserName, Mode = mode });

            var log = await _logEntryService.LogMealInfoAsync(user.Id, userInput);

            TempData["LastMealLogId"] = log.Id.ToString();
            TempData["LastInput"] = userInput;

            return RedirectToAction(nameof(Index), new { mode = LogType.LoggingType.Meal });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Workout(string userInput, LogType.LoggingType mode)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            if (string.IsNullOrWhiteSpace(userInput))
                return View("Log", new LogEntryViewModel { UserName = user.UserName, Mode = mode });

            var log = await _logEntryService.LogWorkoutInfoAsync(user.Id, userInput);

            TempData["LastWorkoutLogId"] = log.Id.ToString();
            TempData["LastInput"] = userInput;

            return RedirectToAction(nameof(Index), new { mode = LogType.LoggingType.Workout });
        }
    }
}
