 using InTakeWise.Models;
using InTakeWise.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.IdentityModel.Abstractions;
using System.Diagnostics;

namespace InTakeWise.Controllers
{
    public class LogEntryController : Controller
    {
        private readonly ILogger<LogEntryController> _logger;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ILogEntryService _logEntryService;


        public LogEntryController(ILogger<LogEntryController> logger, UserManager<IdentityUser> userManager,
        ILogEntryService logEntryService)
        {
            _logger = logger;
            _userManager = userManager;
            _logEntryService = logEntryService;
        }

        [Authorize]
        public async Task<IActionResult> Index(LogType.LoggingType mode)
        {
            var user = await _userManager.GetUserAsync(User);

            return View("Log", new HomeViewModel
            {
                UserName = user?.UserName,
                Mode = mode
            });
        }

        [HttpPost]
        public async Task<IActionResult> Meal(string userInput, LogType.LoggingType mode)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var log = await _logEntryService.CreateMealAsync(user.Id, userInput);

            var vm = new HomeViewModel
            {
                UserName = user.UserName,
                GeneratedLog = log,
                UserInput = userInput,
                Mode = mode
            };

            return View("Log", vm);
        }

        [HttpPost]
        public async Task<IActionResult> Workout(string userInput, LogType.LoggingType mode)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var log = await _logEntryService.CreateWorkoutAsync(user.Id, userInput);

            var vm = new HomeViewModel
            {
                UserName = user.UserName,
                GeneratedLog = log,
                UserInput = userInput,
                Mode = mode
            };

            return View("Log", vm);
        }





        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
