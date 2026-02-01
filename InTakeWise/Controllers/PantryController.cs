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
    public class PantryController : Controller
    {
        private readonly ILogger<LogEntryController> _logger;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ILogEntryService _logEntryService;
        private readonly IFoodItemService _foodItemService;


        public PantryController(ILogger<LogEntryController> logger, UserManager<IdentityUser> userManager,
        ILogEntryService logEntryService, IFoodItemService foodService)
        {
            _logger = logger;
            _userManager = userManager;
            _logEntryService = logEntryService;
            _foodItemService = foodService;
        }

        [Authorize]
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
             
            var foodService = await _foodItemService.GetFoodItemsAsync(User?.Identity?.Name);

            return View("Pantry", new HomeViewModel
            {
                UserName = user?.UserName,
                ItemsInPantry =
                {
                    ItemsAtHome = foodService
                }
            });
        }


        [HttpPost]
        public async Task<IActionResult> SavePantryInfo(string userInput)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            /* Some saving here */

            var foodService = await _foodItemService.GetFoodItemsAsync(User?.Identity?.Name);

            return View("Pantry", new HomeViewModel
            {
                UserName = user?.UserName,
                ItemsInPantry =
                {
                    ItemsAtHome = foodService
                }
            });
        } 
    }
}
