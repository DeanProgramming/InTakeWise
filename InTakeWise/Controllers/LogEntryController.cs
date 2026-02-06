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

            return View("Log", new LogEntryViewModel
            {
                UserName = user?.UserName,
                Mode = mode
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Meal(string userInput, LogType.LoggingType mode)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var log = await _logEntryService.LogMealInfoAsync(user.Id, userInput);

            return View("Log", new LogEntryViewModel
            {
                UserName = user.UserName,
                Mode = mode,
                UserInput = userInput,
                GeneratedLog = log
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Workout(string userInput, LogType.LoggingType mode)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var log = await _logEntryService.LogWorkoutInfoAsync(user.Id, userInput);

            return View("Log", new LogEntryViewModel
            {
                UserName = user.UserName,
                Mode = mode,
                UserInput = userInput,
                GeneratedLog = log
            });
        }
    }
}
