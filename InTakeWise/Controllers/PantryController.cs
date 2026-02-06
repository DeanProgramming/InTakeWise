using InTakeWise.Models;
using InTakeWise.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace InTakeWise.Controllers
{
    [Authorize]
    public class PantryController : Controller
    {
        private readonly ILogger<PantryController> _logger;
        private readonly UserManager<IdentityUser> _userManager;

        public PantryController(
            ILogger<PantryController> logger,
            UserManager<IdentityUser> userManager)
        {
            _logger = logger;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId))
                return Unauthorized();

            var items = new List<PantryItem>
            {
                new PantryItem
                {
                    Id = 1,
                    UserId = userId,
                    FoodItemId = 101,
                    Quantity = 1,
                    Unit = "kg",
                    ExpiryDate = DateTime.UtcNow.AddDays(3),
                    FoodItem = new FoodItem { Id = 101, Name = "Chicken Breast", CaloriesPer100g = 165, ProteinPer100g = 31 }
                },
                new PantryItem
                {
                    Id = 2,
                    UserId = userId,
                    FoodItemId = 102,
                    Quantity = 12,
                    Unit = "pcs",
                    ExpiryDate = DateTime.UtcNow.AddDays(10),
                    FoodItem = new FoodItem { Id = 102, Name = "Eggs", CaloriesPer100g = 155, ProteinPer100g = 13 }
                }
            };

            return View("Pantry", new PantryViewModel
            {
                Items = items
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SavePantryInfo(string userInput)
        {
            var user = await _userManager.GetUserAsync(User);
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (user == null || string.IsNullOrWhiteSpace(userId))
                return Unauthorized();

            var items = new List<PantryItem>
            {
                new PantryItem
                {
                    Id = 3,
                    UserId = userId,
                    FoodItemId = 201,
                    Quantity = 500,
                    Unit = "g",
                    ExpiryDate = DateTime.UtcNow.AddDays(5),
                    FoodItem = new FoodItem { Id = 201, Name = "Greek Yogurt", CaloriesPer100g = 59, ProteinPer100g = 10 }
                }
            };

            return View("Pantry", new PantryViewModel
            {
                Items = items
            });
        }
    }
}
