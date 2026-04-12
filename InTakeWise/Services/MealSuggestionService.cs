using InTakeWise.Data;
using InTakeWise.Dto;
using InTakeWise.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;

namespace InTakeWise.Services
{
    public class MealSuggestionService : IMealSuggestionService
    {
        private readonly ApplicationDbContext _db;
        private readonly IAppClock _clock;

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public MealSuggestionService(ApplicationDbContext db, IAppClock clock)
        {
            _db = db;
            _clock = clock;
        }

        public async Task<TodayMealPlan?> GetTodayPlanAsync(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
                return null;

            var savedPlan = await _db.ShoppingLists
                .AsNoTracking()
                .Include(x => x.Meals)
                .FirstOrDefaultAsync(x => x.UserId == userId);

            if (savedPlan == null || savedPlan.Meals.Count == 0)
                return null;
             
            var today = savedPlan.Meals
                .FirstOrDefault(x => string.Equals(x.Day, _clock.LondonDayOfWeek.ToString(), StringComparison.OrdinalIgnoreCase));

            if (today == null)
                return null;

            var details = DeserializeMealDetails(today.MealDetailsJson);
            var legacySections = ParseMealSections(today.Title);

            return new TodayMealPlan
            {
                Day = today.Day,
                IsGymDay = today.IsGymDay,
                Calories = today.Calories,
                ProteinGrams = today.ProteinGrams,
                CarbsGrams = today.CarbsGrams,
                FatGrams = today.FatGrams,

                Breakfast = BuildMealSuggestion("Breakfast", details.Breakfast)
                            ?? BuildLegacyMealSuggestion("Breakfast", legacySections),

                Lunch = BuildMealSuggestion("Lunch", details.Lunch)
                        ?? BuildLegacyMealSuggestion("Lunch", legacySections),

                Dinner = BuildMealSuggestion("Dinner", details.Dinner)
                         ?? BuildLegacyMealSuggestion("Dinner", legacySections),

                Snack = BuildCombinedSnackSuggestion(details, legacySections)
            };
        }

        private static DailyMealDetailsDto DeserializeMealDetails(string? json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return new DailyMealDetailsDto();

            try
            {
                return JsonSerializer.Deserialize<DailyMealDetailsDto>(json, JsonOptions)
                       ?? new DailyMealDetailsDto();
            }
            catch
            {
                return new DailyMealDetailsDto();
            }
        }

        private static MealSuggestion? BuildMealSuggestion(string mealName, MealDetailDto? detail)
        {
            if (detail == null)
                return null;

            var overview = detail.Overview?.Trim() ?? "";

            var ingredients = (detail.Ingredients ?? new List<string>())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim())
                .ToList();

            var steps = (detail.Steps ?? new List<string>())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim())
                .ToList();

            if (string.IsNullOrWhiteSpace(overview) && ingredients.Count == 0 && steps.Count == 0)
                return null;

            return new MealSuggestion
            {
                Title = mealName,
                Description = overview,
                IngredientsUsed = ingredients,
                Steps = steps
            };
        }

        private static MealSuggestion? BuildLegacyMealSuggestion(string mealName, Dictionary<string, string> sections)
        {
            if (!sections.TryGetValue(mealName, out var description))
                return null;

            if (string.IsNullOrWhiteSpace(description))
                return null;

            return new MealSuggestion
            {
                Title = mealName,
                Description = description.Trim(),
                IngredientsUsed = new List<string>(),
                Steps = new List<string>()
            };
        }

        private static MealSuggestion? BuildCombinedSnackSuggestion(
            DailyMealDetailsDto details,
            Dictionary<string, string> legacySections)
        {
            var snack = BuildMealSuggestion("Snack", details.Snack);
            var lateSnack = BuildMealSuggestion("Late Snack", details.LateSnack);

            if (snack == null && lateSnack == null)
                return BuildCombinedLegacySnackSuggestion(legacySections);

            var overviewParts = new List<string>();

            if (!string.IsNullOrWhiteSpace(snack?.Description))
                overviewParts.Add(snack.Description);

            if (!string.IsNullOrWhiteSpace(lateSnack?.Description))
                overviewParts.Add(lateSnack.Description);

            var ingredients = (snack?.IngredientsUsed ?? new List<string>())
                .Concat(lateSnack?.IngredientsUsed ?? new List<string>())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            var steps = new List<string>();

            if (snack?.Steps != null && snack.Steps.Count > 0)
                steps.AddRange(snack.Steps);

            if (lateSnack?.Steps != null && lateSnack.Steps.Count > 0)
                steps.AddRange(lateSnack.Steps);

            return new MealSuggestion
            {
                Title = "Snack",
                Description = string.Join(Environment.NewLine + Environment.NewLine, overviewParts),
                IngredientsUsed = ingredients,
                Steps = steps
            };
        }

        private static MealSuggestion? BuildCombinedLegacySnackSuggestion(Dictionary<string, string> sections)
        {
            var parts = new List<string>();

            if (sections.TryGetValue("Snack", out var snack) && !string.IsNullOrWhiteSpace(snack))
                parts.Add(snack.Trim());

            if (sections.TryGetValue("Late Snack", out var lateSnack) && !string.IsNullOrWhiteSpace(lateSnack))
                parts.Add(lateSnack.Trim());

            if (parts.Count == 0)
                return null;

            return new MealSuggestion
            {
                Title = "Snack",
                Description = string.Join("\n", parts),
                IngredientsUsed = new List<string>(),
                Steps = new List<string>()
            };
        }

        private static Dictionary<string, string> ParseMealSections(string rawTitle)
        {
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            var formatted = Regex.Replace(
                rawTitle?.Trim() ?? "",
                @"\s*(Late Snack|Breakfast|Lunch|Dinner|Snack):",
                m => (m.Index == 0 ? "" : "\n") + m.Groups[1].Value + ":");

            var lines = formatted
                .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            foreach (var line in lines)
            {
                var idx = line.IndexOf(':');
                if (idx <= 0) continue;

                var key = line[..idx].Trim();
                var value = line[(idx + 1)..].Trim();

                result[key] = value;
            }

            return result;
        }
    }
}