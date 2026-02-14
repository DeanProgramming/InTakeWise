using InTakeWise.Data;
using InTakeWise.Models;
using InTakeWise.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Security.Claims;
using System.Text.RegularExpressions;

namespace InTakeWise.Controllers
{
    [Authorize]
    public class ReceiptController : Controller
    {
        private readonly ILogger<ReceiptController> _logger;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ApplicationDbContext _db; 

        public ReceiptController(
            ILogger<ReceiptController> logger,
            UserManager<IdentityUser> userManager,
            ApplicationDbContext db)
        {
            _logger = logger;
            _userManager = userManager;
            _db = db; 
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View("Receipt", new ReceiptViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult TakePhoto()
        {
            var dummy = string.Join(Environment.NewLine, new[]
            {
                "Chicken Breast - 1 pcs",
                "Basmati Rice - 500 g",
                "Pasta - 500 g",
                "Semi Skimmed Milk - 1 L",
                "Eggs - 12 pcs"
            });

            return View("Receipt", new ReceiptViewModel
            {
                DecodedInfo = dummy
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveToPantryInfo(ReceiptViewModel vm)
        {
            var user = await _userManager.GetUserAsync(User);
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (user == null || string.IsNullOrWhiteSpace(userId))
                return Unauthorized();

            var parsedLines = ParsePantryInput(vm.DecodedInfo);

            var existingPantry = await _db.PantryItems
                .Include(x => x.FoodItem)
                .Where(x => x.UserId == userId)
                .ToListAsync();

            var keepFoodItemIds = new HashSet<int>();

            foreach (var line in parsedLines)
            {
                var food = await _db.FoodItems
                    .FirstOrDefaultAsync(f => f.Name.ToLower() == line.Name.ToLower());

                if (food == null)
                {
                    food = new FoodItem { Name = line.Name };
                    _db.FoodItems.Add(food);
                    await _db.SaveChangesAsync();
                }

                keepFoodItemIds.Add(food.Id);

                var pantryItem = existingPantry.FirstOrDefault(p => p.FoodItemId == food.Id);
                if (pantryItem == null)
                {
                    _db.PantryItems.Add(new PantryItem
                    {
                        UserId = userId,
                        FoodItemId = food.Id,
                        Quantity = line.Quantity,
                        Unit = line.Unit ?? "",
                        ExpiryDate = line.ExpiryDate
                    });
                }
                else
                {
                    pantryItem.Quantity = line.Quantity;
                    pantryItem.Unit = line.Unit ?? "";
                    pantryItem.ExpiryDate = line.ExpiryDate;
                }
            }

            var toRemove = existingPantry
                .Where(p => !keepFoodItemIds.Contains(p.FoodItemId))
                .ToList();

            if (toRemove.Count > 0)
                _db.PantryItems.RemoveRange(toRemove);

            await _db.SaveChangesAsync();
            TempData["PantrySaved"] = "Pantry saved.";

            return RedirectToAction("Index", "Pantry");
        }

        private sealed record ParsedPantryLine(string Name, decimal Quantity, string Unit, DateTime? ExpiryDate);

        private static List<ParsedPantryLine> ParsePantryInput(string? input)
        {
            var results = new List<ParsedPantryLine>();
            if (string.IsNullOrWhiteSpace(input))
                return results;

            var lines = input.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);

            foreach (var raw in lines)
            {
                var line = (raw ?? "").Trim();
                if (string.IsNullOrWhiteSpace(line)) continue;

                DateTime? expiry = null;
                var expiryMatch = Regex.Match(line, @"\((?:Expires|Expiry)\s*:\s*(\d{4}-\d{2}-\d{2})\)\s*$",
                    RegexOptions.IgnoreCase);

                if (expiryMatch.Success)
                {
                    var dateText = expiryMatch.Groups[1].Value;
                    if (DateTime.TryParseExact(dateText, "yyyy-MM-dd", CultureInfo.InvariantCulture,
                        DateTimeStyles.None, out var dt))
                    {
                        expiry = dt.Date;
                    }

                    line = Regex.Replace(line, @"\s*\((?:Expires|Expiry)\s*:\s*\d{4}-\d{2}-\d{2}\)\s*$", "",
                        RegexOptions.IgnoreCase).Trim();
                }

                var parts = line.Split(" - ", 2, StringSplitOptions.TrimEntries);
                var name = parts[0].Trim();

                decimal qty = 1;
                string unit = "";

                if (parts.Length == 2)
                {
                    var rest = parts[1].Trim();
                    var restParts = rest.Split(' ', 2, StringSplitOptions.TrimEntries);

                    if (restParts.Length >= 1)
                    {
                        if (!decimal.TryParse(restParts[0], NumberStyles.Number, CultureInfo.InvariantCulture, out qty))
                        {
                            decimal.TryParse(restParts[0], NumberStyles.Number, CultureInfo.CurrentCulture, out qty);
                        }
                    }

                    if (restParts.Length == 2)
                        unit = restParts[1].Trim();
                }

                if (string.IsNullOrWhiteSpace(name))
                    continue;

                results.Add(new ParsedPantryLine(name, qty, unit, expiry));
            }

            return results
                .GroupBy(x => x.Name.Trim(), StringComparer.OrdinalIgnoreCase)
                .Select(g =>
                {
                    var last = g.Last();
                    return new ParsedPantryLine(
                        Name: g.Key,
                        Quantity: g.Sum(x => x.Quantity),
                        Unit: last.Unit,
                        ExpiryDate: last.ExpiryDate
                    );
                })
                .ToList();
        }
    }
}
