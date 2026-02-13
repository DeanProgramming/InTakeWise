using InTakeWise.Data;
using InTakeWise.Models;
using InTakeWise.Services;
using InTakeWise.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace InTakeWise.Controllers
{
    public class HomeController : BaseController
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ILogEntryService _logEntryService;

        public HomeController(
            ILogger<HomeController> logger,
            ApplicationDbContext db,
            UserManager<IdentityUser> userManager,
            ILogEntryService logEntryService)
            : base(db, userManager)
        {
            _logger = logger;
            _logEntryService = logEntryService;
        }

        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var vm = new HomeViewModel
            {
                LoggedBreakfast = await _logEntryService.GetCompletedTodayMealAsync(user.Id, TimeOfDay.Breakfast),
                LoggedDinner = await _logEntryService.GetCompletedTodayMealAsync(user.Id, TimeOfDay.Dinner),
                LoggedTea = await _logEntryService.GetCompletedTodayMealAsync(user.Id, TimeOfDay.Tea)
            }; 

            return View("Index", vm);
        }

        public IActionResult Privacy() => View();

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
