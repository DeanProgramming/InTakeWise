using InTakeWise.Data;
using InTakeWise.Models;
using InTakeWise.Security;
using InTakeWise.Services;
using InTakeWise.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
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
        private readonly IReceiptImageAnalyzer _receiptImageAnalyzer;

        public ReceiptController(
            ILogger<ReceiptController> logger,
            UserManager<IdentityUser> userManager,
            ApplicationDbContext db,
            IPantryUnitService pantryUnitService,
            IReceiptImageAnalyzer receiptImageAnalyzer)
        {
            _logger = logger;
            _userManager = userManager;
            _db = db;
            _pantryUnitService = pantryUnitService;
            _receiptImageAnalyzer = receiptImageAnalyzer;
        }

        [HttpGet]
        public IActionResult Index(string? returnUrl = null)
        {
            var model = CreateReceiptModel(returnUrl);
            EnsureBlankRow(model);
            return View("Receipt", model);
        }

        [HttpGet]
        public IActionResult LoadSampleReceipt(string? returnUrl = null)
        {
            var model = CreateReceiptModel(returnUrl);
            model.IsSample = true;
            model.Items = new List<PantryItemInputViewModel>
            {
                new() { Name = "Chicken Breast", Quantity = 1, Unit = "items" },
                new() { Name = "Basmati Rice", Quantity = 500, Unit = "g" },
                new() { Name = "Pasta", Quantity = 500, Unit = "g" },
                new() { Name = "Semi-Skimmed Milk", Quantity = 1, Unit = "l" },
                new() { Name = "Eggs", Quantity = 12, Unit = "items" }
            };

            EnsureBlankRow(model);
            return View("Receipt", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [EnableRateLimiting("receipt-analysis")]
        [RequestFormLimits(MultipartBodyLengthLimit = ReceiptImageUpload.MaxRequestBytes)]
        [RequestSizeLimit(ReceiptImageUpload.MaxRequestBytes)]
        public async Task<IActionResult> AnalyzePhoto(
            IFormFile? receiptImage,
            string? returnUrl = null,
            CancellationToken cancellationToken = default)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrWhiteSpace(userId))
                return Unauthorized();

            var model = CreateReceiptModel(returnUrl);
            var upload = await ReceiptImageUpload.ReadAsync(receiptImage, cancellationToken);

            if (!upload.IsValid)
            {
                ModelState.AddModelError("", upload.Error ?? "The receipt photo is invalid.");
                EnsureBlankRow(model);
                return View("Receipt", model);
            }

            try
            {
                var analysis = await _receiptImageAnalyzer.AnalyzeAsync(
                    userId,
                    upload.Bytes!,
                    upload.MediaType!,
                    cancellationToken);

                if (!analysis.IsReceipt)
                {
                    ModelState.AddModelError(
                        "",
                        "That image does not look like a grocery receipt. Try a clear photo of the full receipt.");
                }
                else if (!analysis.IsReadable)
                {
                    ModelState.AddModelError(
                        "",
                        "A receipt was detected, but its item lines were not clear enough to read. Try a sharper, well-lit photo.");
                }
                else if (analysis.Items.Count == 0)
                {
                    ModelState.AddModelError(
                        "",
                        "No food or drink items could be extracted from that receipt.");
                }
                else
                {
                    model.WasPhotoAnalysed = true;
                    model.Items = analysis.Items
                        .Select(item => new PantryItemInputViewModel
                        {
                            Name = item.Name,
                            Quantity = item.Quantity,
                            Unit = item.Unit
                        })
                        .ToList();

                    _logger.LogInformation(
                        "Extracted {ItemCount} pantry candidates from a receipt for user {UserId}.",
                        model.Items.Count,
                        userId);
                }
            }
            catch (DemoAiAccessDeniedException ex)
            {
                ModelState.AddModelError("", ex.Message);
            }
            catch (ReceiptImageAnalysisException ex)
            {
                _logger.LogWarning(
                    ex,
                    "Receipt photo analysis could not be completed for user {UserId}.",
                    userId);

                ModelState.AddModelError("", ex.Message);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Unexpected receipt photo analysis failure for user {UserId}.",
                    userId);

                ModelState.AddModelError(
                    "",
                    "Receipt photo analysis is temporarily unavailable. Please try again.");
            }

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
                else if (row.Name.Trim().Length > 120)
                    ModelState.AddModelError($"Items[{i}].Name", "Food must be 120 characters or fewer.");

                if (!row.Quantity.HasValue || row.Quantity <= 0)
                    ModelState.AddModelError($"Items[{i}].Quantity", "Amount is required.");
                else if (row.Quantity > 100_000)
                    ModelState.AddModelError($"Items[{i}].Quantity", "Amount must be 100,000 or less.");

                if (string.IsNullOrWhiteSpace(row.Unit))
                    ModelState.AddModelError($"Items[{i}].Unit", "Unit is required.");
                else if (row.Unit.Trim().Length > 30)
                    ModelState.AddModelError($"Items[{i}].Unit", "Unit must be 30 characters or fewer.");
            }

            if (rows.Count == 0)
            {
                ModelState.AddModelError("", "Add at least one receipt item.");
            }
            else if (rows.Count > 100)
            {
                ModelState.AddModelError("", "A receipt can contain at most 100 pantry items.");
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

                TempData["PantrySaved"] = model.IsSample
                    ? "Fictional sample receipt items added to pantry."
                    : "Receipt items added to pantry.";

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

        private ReceiptViewModel CreateReceiptModel(string? returnUrl)
        {
            return new ReceiptViewModel
            {
                ReturnUrl = GetSafeReturnUrl(returnUrl)
            };
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
    }
}
