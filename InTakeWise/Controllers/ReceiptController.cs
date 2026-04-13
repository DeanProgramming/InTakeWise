using InTakeWise.Data;
using InTakeWise.Models;
using InTakeWise.Services;
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
        private readonly IPantryUnitService _pantryUnitService;

        public ReceiptController(
            ILogger<ReceiptController> logger,
            UserManager<IdentityUser> userManager,
            ApplicationDbContext db,
            IPantryUnitService pantryUnitService)
        {
            _logger = logger;
            _userManager = userManager;
            _db = db;
            _pantryUnitService = pantryUnitService;
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

            var mergeResult = _pantryUnitService.MergeRowsByFoodName(rows);

            if (mergeResult.HasErrors)
            {
                foreach (var error in mergeResult.Errors)
                {
                    ModelState.AddModelError("", error);
                }

                model.Items = rows;
                EnsureBlankRow(model);
                return View("Receipt", model);
            }

            var mergedReceiptRows = mergeResult.Rows;

            await using var tx = await _db.Database.BeginTransactionAsync();

            try
            {
                var foodMap = await GetOrCreateFoodItemsAsync(mergedReceiptRows.Select(x => x.Name));

                await _db.SaveChangesAsync();

                var existingPantry = await _db.PantryItems
                    .Include(x => x.FoodItem)
                    .Where(x => x.UserId == userId)
                    .ToListAsync();

                foreach (var receiptRow in mergedReceiptRows)
                {
                    var cleanName = FoodItemNameNormalizer.CleanDisplayName(receiptRow.Name);
                    var normalizedName = FoodItemNameNormalizer.NormalizeName(receiptRow.Name);

                    var food = foodMap[normalizedName];

                    var existingMatches = existingPantry
                        .Where(x => x.FoodItem != null &&
                                    x.FoodItem.NormalizedName == normalizedName)
                        .ToList();

                    if (existingMatches.Count == 0)
                    {
                        var newPantryItem = new PantryItem
                        {
                            UserId = userId,
                            FoodItemId = food.Id,
                            Quantity = receiptRow.Quantity ?? 0,
                            Unit = receiptRow.Unit.Trim()
                        };

                        _db.PantryItems.Add(newPantryItem);
                        existingPantry.Add(newPantryItem);
                        continue;
                    }

                    var mergeExistingResult = _pantryUnitService.MergeWithExisting(
                        cleanName,
                        receiptRow.Quantity ?? 0m,
                        receiptRow.Unit,
                        existingMatches.Select(x => new PantryQuantityItem
                        {
                            Quantity = x.Quantity,
                            Unit = x.Unit
                        }));

                    if (mergeExistingResult.HasErrors)
                    {
                        foreach (var error in mergeExistingResult.Errors)
                        {
                            ModelState.AddModelError("", error);
                        }

                        break;
                    }

                    var keeper = existingMatches.First();
                    keeper.FoodItemId = food.Id;
                    keeper.Quantity = mergeExistingResult.Quantity;
                    keeper.Unit = mergeExistingResult.Unit;

                    var duplicates = existingMatches.Skip(1).ToList();
                    if (duplicates.Count > 0)
                    {
                        _db.PantryItems.RemoveRange(duplicates);

                        foreach (var duplicate in duplicates)
                        {
                            existingPantry.Remove(duplicate);
                        }
                    }
                }

                if (!ModelState.IsValid)
                {
                    await tx.RollbackAsync();
                    model.Items = rows;
                    EnsureBlankRow(model);
                    return View("Receipt", model);
                }

                await _db.SaveChangesAsync();
                await tx.CommitAsync();

                TempData["PantrySaved"] = "Receipt items added to pantry.";

                return RedirectToAction("Index", "Pantry", new
                {
                    returnUrl = GetPantryParentReturnUrl(model.ReturnUrl)
                });
            }
            catch (DbUpdateException ex)
            {
                await tx.RollbackAsync();
                _logger.LogError(ex, "Error saving receipt items to pantry for user {UserId}", userId);

                TempData["PantrySaved"] = "Could not save receipt items (database error).";
                EnsureBlankRow(model);
                return View("Receipt", model);
            }
        }

        private async Task<Dictionary<string, FoodItem>> GetOrCreateFoodItemsAsync(
            IEnumerable<string> rawNames,
            CancellationToken cancellationToken = default)
                {
                    var normalizedNames = rawNames
                        .Where(x => !string.IsNullOrWhiteSpace(x))
                        .Select(FoodItemNameNormalizer.NormalizeName)
                        .Distinct(StringComparer.Ordinal)
                        .ToList();

                    var existingFoods = await _db.FoodItems
                        .Where(x => normalizedNames.Contains(x.NormalizedName))
                        .ToListAsync(cancellationToken);

                    var foodMap = existingFoods.ToDictionary(x => x.NormalizedName, StringComparer.Ordinal);

                    foreach (var rawName in rawNames.Where(x => !string.IsNullOrWhiteSpace(x)))
                    {
                        var cleanName = FoodItemNameNormalizer.CleanDisplayName(rawName);
                        var normalizedName = FoodItemNameNormalizer.NormalizeName(rawName);

                        if (foodMap.ContainsKey(normalizedName))
                            continue;

                        var newFood = new FoodItem
                        {
                            Name = cleanName,
                            NormalizedName = normalizedName
                        };

                        _db.FoodItems.Add(newFood);
                        foodMap[normalizedName] = newFood;
                    }

                    return foodMap;
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
    }
}