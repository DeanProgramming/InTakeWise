using InTakeWise.Data;
using InTakeWise.Models;
using InTakeWise.Services;
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
        private readonly IPantryUnitService _pantryUnitService;

        public PantryController(
            ILogger<PantryController> logger,
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

            var mergeResult = _pantryUnitService.MergeRowsByFoodName(rows);

            if (mergeResult.HasErrors)
            {
                foreach (var error in mergeResult.Errors)
                {
                    ModelState.AddModelError("", error);
                }

                if (!model.Items.Any() ||
                    !string.IsNullOrWhiteSpace(model.Items.Last().Name) ||
                    model.Items.Last().Quantity.HasValue ||
                    !string.IsNullOrWhiteSpace(model.Items.Last().Unit))
                {
                    model.Items.Add(new PantryItemInputViewModel());
                }

                return View("Pantry", model);
            }

            var mergedRows = mergeResult.Rows;

            await using var tx = await _db.Database.BeginTransactionAsync();

            try
            {
                var foodMap = await GetOrCreateFoodItemsAsync(mergedRows.Select(x => x.Name));

                await _db.SaveChangesAsync();

                var existingPantry = await _db.PantryItems
                    .Include(x => x.FoodItem)
                    .Where(x => x.UserId == userId)
                    .ToListAsync();

                var keepIds = new HashSet<int>();

                foreach (var row in mergedRows)
                {
                    var normalizedName = FoodItemNameNormalizer.NormalizeName(row.Name);
                    var food = foodMap[normalizedName];

                    var existing = row.Id > 0
                        ? existingPantry.FirstOrDefault(x => x.Id == row.Id)
                        : null;

                    existing ??= existingPantry.FirstOrDefault(x =>
                        x.FoodItem != null &&
                        x.FoodItem.NormalizedName == normalizedName);

                    if (existing == null)
                    {
                        var newPantryItem = new PantryItem
                        {
                            UserId = userId,
                            FoodItemId = food.Id,
                            Quantity = row.Quantity ?? 0,
                            Unit = row.Unit?.Trim() ?? ""
                        };

                        _db.PantryItems.Add(newPantryItem);
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

                // Save pantry changes
                await _db.SaveChangesAsync();
                await tx.CommitAsync();

                TempData["PantrySaved"] = "Pantry saved.";
            }
            catch (DbUpdateException ex)
            {
                await tx.RollbackAsync();
                _logger.LogError(ex, "Error saving pantry for user {UserId}", userId);
                TempData["PantrySaved"] = "Could not save pantry (database error).";

                if (!model.Items.Any() ||
                    !string.IsNullOrWhiteSpace(model.Items.Last().Name) ||
                    model.Items.Last().Quantity.HasValue ||
                    !string.IsNullOrWhiteSpace(model.Items.Last().Unit))
                {
                    model.Items.Add(new PantryItemInputViewModel());
                }

                return View("Pantry", model);
            }

            return RedirectToAction(nameof(Index), new { returnUrl = model.ReturnUrl });
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
    }
}