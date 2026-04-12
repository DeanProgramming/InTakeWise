using InTakeWise.Data;
using InTakeWise.Models;
using InTakeWise.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace InTakeWise.Controllers
{
    [Authorize]
    public class PantryController : Controller
    {
        private readonly ILogger<PantryController> _logger;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ApplicationDbContext _db;

        public PantryController(
            ILogger<PantryController> logger,
            UserManager<IdentityUser> userManager,
            ApplicationDbContext db)
        {
            _logger = logger;
            _userManager = userManager;
            _db = db;
        }

        [HttpGet]
        public async Task<IActionResult> Index(string? returnUrl = null)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId))
                return Unauthorized();

            var items = await _db.PantryItems
                .AsNoTracking()
                .Include(x => x.FoodItem)
                .Where(x => x.UserId == userId)
                .OrderBy(x => x.FoodItem.Name)
                .ToListAsync();

            var model = new PantryViewModel
            {
                ReturnUrl = Url.IsLocalUrl(returnUrl)
                    ? returnUrl
                    : Url.Action("Index", "Home"),

                Items = items.Select(x => new PantryItemInputViewModel
                {
                    Id = x.Id,
                    FoodItemId = x.FoodItemId,
                    Name = x.FoodItem?.Name ?? "",
                    Quantity = x.Quantity,
                    Unit = x.Unit ?? ""
                }).ToList()
            };

            model.Items.Add(new PantryItemInputViewModel());

            return View("Pantry", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SavePantryInfo(PantryViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (user == null || string.IsNullOrWhiteSpace(userId))
                return Unauthorized();

            model.ReturnUrl = Url.IsLocalUrl(model.ReturnUrl)
                ? model.ReturnUrl
                : Url.Action("Index", "Home");

            model.Items ??= new List<PantryItemInputViewModel>();

            var rows = model.Items
                .Where(x =>
                    !string.IsNullOrWhiteSpace(x.Name) ||
                    x.Quantity.HasValue ||
                    !string.IsNullOrWhiteSpace(x.Unit) ||
                    x.Id > 0)
                .ToList();

            if (!rows.Any(x => x.Id == 0 &&
                               string.IsNullOrWhiteSpace(x.Name) &&
                               !x.Quantity.HasValue &&
                               string.IsNullOrWhiteSpace(x.Unit)))
            {
                model.Items = rows.ToList();
            }

            if (!ModelState.IsValid)
            {
                if (!model.Items.Any() ||
                    !string.IsNullOrWhiteSpace(model.Items.Last().Name) ||
                    model.Items.Last().Quantity.HasValue ||
                    !string.IsNullOrWhiteSpace(model.Items.Last().Unit))
                {
                    model.Items.Add(new PantryItemInputViewModel());
                }

                return View("Pantry", model);
            }

            var mergedRows = new List<PantryItemInputViewModel>();

            foreach (var group in rows
                .Where(x => !string.IsNullOrWhiteSpace(x.Name))
                .GroupBy(x => x.Name.Trim(), StringComparer.OrdinalIgnoreCase))
            {
                UnitInfo? firstUnit = null;
                decimal totalBaseQuantity = 0m;

                foreach (var row in group)
                {
                    var parsedUnit = ParseUnit(row.Unit);
                    var qty = row.Quantity ?? 0m;

                    if (firstUnit == null)
                    {
                        firstUnit = parsedUnit;
                    }
                    else if (!string.Equals(firstUnit.Family, parsedUnit.Family, StringComparison.OrdinalIgnoreCase))
                    {
                        ModelState.AddModelError("", $"'{group.Key}' has incompatible units ('{firstUnit.NormalizedUnit}' and '{parsedUnit.NormalizedUnit}').");
                        continue;
                    }

                    totalBaseQuantity += qty * parsedUnit.FactorToBase;
                }

                if (firstUnit == null)
                    continue;

                var converted = ConvertFromBase(totalBaseQuantity, firstUnit.Family, firstUnit.NormalizedUnit);

                mergedRows.Add(new PantryItemInputViewModel
                {
                    Id = group.Where(x => x.Id > 0).Select(x => x.Id).FirstOrDefault(),
                    FoodItemId = group.Where(x => x.FoodItemId > 0).Select(x => x.FoodItemId).FirstOrDefault(),
                    Name = group.Key,
                    Quantity = converted.Quantity,
                    Unit = converted.Unit
                });
            }

            if (!ModelState.IsValid)
            {
                if (!model.Items.Any() ||
                    !string.IsNullOrWhiteSpace(model.Items.Last().Name) ||
                    model.Items.Last().Quantity.HasValue ||
                    !string.IsNullOrWhiteSpace(model.Items.Last().Unit))
                {
                    model.Items.Add(new PantryItemInputViewModel());
                }

                return View("Pantry", model);
            }

            var existingPantry = await _db.PantryItems
                .Include(x => x.FoodItem)
                .Where(x => x.UserId == userId)
                .ToListAsync();

            var keepIds = new HashSet<int>();

            foreach (var row in mergedRows)
            {
                var trimmedName = row.Name.Trim();

                var food = await _db.FoodItems
                    .FirstOrDefaultAsync(f => f.Name.ToLower() == trimmedName.ToLower());

                if (food == null)
                {
                    food = new FoodItem { Name = trimmedName };
                    _db.FoodItems.Add(food);
                    await _db.SaveChangesAsync();
                }

                var existing = row.Id > 0
                    ? existingPantry.FirstOrDefault(x => x.Id == row.Id)
                    : null;

                existing ??= existingPantry.FirstOrDefault(x =>
                    x.FoodItem != null &&
                    x.FoodItem.Name.Equals(trimmedName, StringComparison.OrdinalIgnoreCase));

                if (existing == null)
                {
                    _db.PantryItems.Add(new PantryItem
                    {
                        UserId = userId,
                        FoodItemId = food.Id,
                        Quantity = row.Quantity ?? 0,
                        Unit = row.Unit?.Trim() ?? ""
                    });
                }
                else
                {
                    existing.FoodItemId = food.Id;
                    existing.Quantity = row.Quantity ?? 0;
                    existing.Unit = row.Unit?.Trim() ?? "";

                    keepIds.Add(existing.Id);
                }
            }

            var toRemove = existingPantry
                .Where(x => !keepIds.Contains(x.Id))
                .ToList();

            if (toRemove.Count > 0)
                _db.PantryItems.RemoveRange(toRemove);

            try
            {
                await _db.SaveChangesAsync();
                TempData["PantrySaved"] = "Pantry saved.";
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Error saving pantry for user {UserId}", userId);
                TempData["PantrySaved"] = "Could not save pantry (database error).";
            }

            return RedirectToAction(nameof(Index), new { returnUrl = model.ReturnUrl });
        }

        private sealed record UnitInfo(string NormalizedUnit, string Family, decimal FactorToBase);

        private static UnitInfo ParseUnit(string? rawUnit)
        {
            var unit = (rawUnit ?? "").Trim().ToLowerInvariant();

            return unit switch
            {
                "mg" or "milligram" or "milligrams"
                    => new UnitInfo("mg", "mass", 0.001m),

                "g" or "gram" or "grams"
                    => new UnitInfo("g", "mass", 1m),

                "kg" or "kilogram" or "kilograms"
                    => new UnitInfo("kg", "mass", 1000m),

                "ml" or "millilitre" or "millilitres" or "milliliter" or "milliliters"
                    => new UnitInfo("ml", "volume", 1m),

                "l" or "litre" or "litres" or "liter" or "liters"
                    => new UnitInfo("l", "volume", 1000m),

                "tin" or "tins"
                    => new UnitInfo("tins", "count:tins", 1m),

                "can" or "cans"
                    => new UnitInfo("cans", "count:cans", 1m),

                "pack" or "packs"
                    => new UnitInfo("packs", "count:packs", 1m),

                "bottle" or "bottles"
                    => new UnitInfo("bottles", "count:bottles", 1m),

                "item" or "items" or "piece" or "pieces" or "unit" or "units"
                    => new UnitInfo("items", "count:items", 1m),

                _ => new UnitInfo(unit, $"custom:{unit}", 1m)
            };
        }

        private static (decimal Quantity, string Unit) ConvertFromBase(decimal totalBase, string family, string fallbackUnit)
        {
            if (family == "mass")
            {
                if (totalBase >= 1000m)
                    return (decimal.Round(totalBase / 1000m, 3), "kg");

                if (totalBase >= 1m)
                    return (decimal.Round(totalBase, 3), "g");

                return (decimal.Round(totalBase * 1000m, 3), "mg");
            }

            if (family == "volume")
            {
                if (totalBase >= 1000m)
                    return (decimal.Round(totalBase / 1000m, 3), "l");

                return (decimal.Round(totalBase, 3), "ml");
            }

            return (decimal.Round(totalBase, 3), fallbackUnit);
        }
    }
}