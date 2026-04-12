using InTakeWise.Data;
using InTakeWise.Models;
using InTakeWise.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Security.Claims;

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
        public IActionResult Index(string? returnUrl = null)
        {
            var model = new ReceiptViewModel
            {
                ReturnUrl = GetSafeReturnUrl(returnUrl)
            };

            EnsureBlankRow(model);
            return View("Receipt", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult TakePhoto(string? returnUrl = null)
        {
            var dummy = string.Join(Environment.NewLine, new[]
            {
                "Chicken Breast - 1 pcs",
                "Basmati Rice - 500 g",
                "Pasta - 500 g",
                "Semi Skimmed Milk - 1 L",
                "Eggs - 12 pcs"
            });

            var model = new ReceiptViewModel
            {
                ReturnUrl = GetSafeReturnUrl(returnUrl),
                Items = ParseReceiptInput(dummy)
            };

            EnsureBlankRow(model);

            return View("Receipt", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveToPantryInfo(ReceiptViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (user == null || string.IsNullOrWhiteSpace(userId))
                return Unauthorized();

            model.ReturnUrl = GetSafeReturnUrl(model.ReturnUrl);
            model.Items ??= new List<PantryItemInputViewModel>();

            var rows = model.Items
                .Where(RowHasAnyValue)
                .ToList();

            for (int i = 0; i < model.Items.Count; i++)
            {
                var row = model.Items[i];

                if (!RowHasAnyValue(row))
                    continue;

                if (string.IsNullOrWhiteSpace(row.Name))
                    ModelState.AddModelError($"Items[{i}].Name", "Food is required.");

                if (!row.Quantity.HasValue || row.Quantity <= 0)
                    ModelState.AddModelError($"Items[{i}].Quantity", "Amount is required.");

                if (string.IsNullOrWhiteSpace(row.Unit))
                    ModelState.AddModelError($"Items[{i}].Unit", "Unit is required.");
            }

            if (rows.Count == 0)
            {
                ModelState.AddModelError("", "Add at least one receipt item.");
            }

            if (!ModelState.IsValid)
            {
                EnsureBlankRow(model);
                return View("Receipt", model);
            }

            var mergedReceiptRows = new List<PantryItemInputViewModel>();

            foreach (var group in rows.GroupBy(x => x.Name.Trim(), StringComparer.OrdinalIgnoreCase))
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

                mergedReceiptRows.Add(new PantryItemInputViewModel
                {
                    Name = group.Key,
                    Quantity = converted.Quantity,
                    Unit = converted.Unit
                });
            }

            if (!ModelState.IsValid)
            {
                model.Items = rows;
                EnsureBlankRow(model);
                return View("Receipt", model);
            }

            var existingPantry = await _db.PantryItems
                .Include(x => x.FoodItem)
                .Where(x => x.UserId == userId)
                .ToListAsync();

            foreach (var receiptRow in mergedReceiptRows)
            {
                var trimmedName = receiptRow.Name.Trim();

                var food = await _db.FoodItems
                    .FirstOrDefaultAsync(f => f.Name.ToLower() == trimmedName.ToLower());

                if (food == null)
                {
                    food = new FoodItem { Name = trimmedName };
                    _db.FoodItems.Add(food);
                    await _db.SaveChangesAsync();
                }

                var existingMatches = existingPantry
                    .Where(x => x.FoodItem != null &&
                                x.FoodItem.Name.Equals(trimmedName, StringComparison.OrdinalIgnoreCase))
                    .ToList();

                if (existingMatches.Count == 0)
                {
                    _db.PantryItems.Add(new PantryItem
                    {
                        UserId = userId,
                        FoodItemId = food.Id,
                        Quantity = receiptRow.Quantity ?? 0,
                        Unit = receiptRow.Unit.Trim()
                    });

                    continue;
                }

                var baseTotal = 0m;

                foreach (var pantryItem in existingMatches)
                {
                    var parsedExistingUnit = ParseUnit(pantryItem.Unit);
                    var parsedReceiptUnit = ParseUnit(receiptRow.Unit);

                    if (!string.Equals(parsedExistingUnit.Family, parsedReceiptUnit.Family, StringComparison.OrdinalIgnoreCase))
                    {
                        ModelState.AddModelError("", $"Cannot combine pantry item '{trimmedName}' because units are incompatible ('{pantryItem.Unit}' and '{receiptRow.Unit}').");
                        break;
                    }

                    baseTotal += pantryItem.Quantity * parsedExistingUnit.FactorToBase;
                }

                if (!ModelState.IsValid)
                    break;

                var receiptParsed = ParseUnit(receiptRow.Unit);
                baseTotal += (receiptRow.Quantity ?? 0m) * receiptParsed.FactorToBase;

                var converted = ConvertFromBase(baseTotal, receiptParsed.Family, receiptParsed.NormalizedUnit);

                var keeper = existingMatches.First();
                keeper.FoodItemId = food.Id;
                keeper.Quantity = converted.Quantity;
                keeper.Unit = converted.Unit;

                var duplicates = existingMatches.Skip(1).ToList();
                if (duplicates.Count > 0)
                {
                    _db.PantryItems.RemoveRange(duplicates);
                }
            }

            if (!ModelState.IsValid)
            {
                model.Items = rows;
                EnsureBlankRow(model);
                return View("Receipt", model);
            }

            try
            {
                await _db.SaveChangesAsync();
                TempData["PantrySaved"] = "Receipt items added to pantry.";
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Error saving receipt items to pantry for user {UserId}", userId);
                TempData["PantrySaved"] = "Could not save receipt items (database error).";
                EnsureBlankRow(model);
                return View("Receipt", model);
            }

            return RedirectToAction("Index", "Pantry", new
            {
                returnUrl = GetPantryParentReturnUrl(model.ReturnUrl)
            });
        }

        private string GetSafeReturnUrl(string? returnUrl)
        {
            return Url.IsLocalUrl(returnUrl)
                ? returnUrl!
                : Url.Action("Index", "Home")!;
        }

        private string GetPantryParentReturnUrl(string? receiptReturnUrl)
        {
            var safeDefault = Url.Action("Index", "Home")!;
            var safeReturnUrl = GetSafeReturnUrl(receiptReturnUrl);

            var pantryPath = Url.Action("Index", "Pantry") ?? "/Pantry";
            var absolute = new Uri(new Uri($"{Request.Scheme}://{Request.Host}"), safeReturnUrl);

            if (!absolute.AbsolutePath.Equals(pantryPath, StringComparison.OrdinalIgnoreCase))
                return safeReturnUrl;

            var query = QueryHelpers.ParseQuery(absolute.Query);

            if (query.TryGetValue("returnUrl", out var nested))
            {
                var nestedReturnUrl = nested.FirstOrDefault();

                if (Url.IsLocalUrl(nestedReturnUrl))
                    return nestedReturnUrl!;
            }

            return safeDefault;
        }

        private static bool RowHasAnyValue(PantryItemInputViewModel item)
        {
            return !string.IsNullOrWhiteSpace(item.Name)
                || item.Quantity.HasValue
                || !string.IsNullOrWhiteSpace(item.Unit);
        }

        private static void EnsureBlankRow(ReceiptViewModel model)
        {
            model.Items ??= new List<PantryItemInputViewModel>();

            if (model.Items.Count == 0 || RowHasAnyValue(model.Items.Last()))
            {
                model.Items.Add(new PantryItemInputViewModel());
            }
        }

        private static List<PantryItemInputViewModel> ParseReceiptInput(string? input)
        {
            var results = new List<PantryItemInputViewModel>();
            if (string.IsNullOrWhiteSpace(input))
                return results;

            var lines = input.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);

            foreach (var raw in lines)
            {
                var line = (raw ?? "").Trim();
                if (string.IsNullOrWhiteSpace(line))
                    continue;

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

                results.Add(new PantryItemInputViewModel
                {
                    Name = name,
                    Quantity = qty,
                    Unit = unit
                });
            }

            return results;
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

                "pcs" or "pc" or "piece" or "pieces"
                    => new UnitInfo("pcs", "count:pcs", 1m),

                "tin" or "tins"
                    => new UnitInfo("tins", "count:tins", 1m),

                "can" or "cans"
                    => new UnitInfo("cans", "count:cans", 1m),

                "pack" or "packs"
                    => new UnitInfo("packs", "count:packs", 1m),

                "bottle" or "bottles"
                    => new UnitInfo("bottles", "count:bottles", 1m),

                "item" or "items" or "unit" or "units"
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