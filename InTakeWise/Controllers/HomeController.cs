using InTakeWise.Helper;
using InTakeWise.Models;
using InTakeWise.Services;
using InTakeWise.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace InTakeWise.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ILogEntryService _logEntryService;

        public HomeController(
            ILogger<HomeController> logger,
            UserManager<IdentityUser> userManager,
            ILogEntryService logEntryService)
        {
            _logger = logger;
            _userManager = userManager;
            _logEntryService = logEntryService;
        }

        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var vm = new HomeViewModel
            {
                LoggedBreakfast = await _logEntryService.GetCompletedTodayMealAsync(user.Id, MealType.Breakfast),
                LoggedDinner = await _logEntryService.GetCompletedTodayMealAsync(user.Id, MealType.Dinner),
                LoggedTea = await _logEntryService.GetCompletedTodayMealAsync(user.Id, MealType.Tea)
            };

            return View(vm);
        }
        [AllowAnonymous]
        public IActionResult Privacy() => View();

        [AllowAnonymous]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel
            {
                RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
            });
        }
    }
}