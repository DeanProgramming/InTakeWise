using System.Text;
using System.Text.Json;
using InTakeWise.Data;
using InTakeWise.Dto;
using InTakeWise.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OpenAI.Chat;

namespace InTakeWise.Services
{
    public class ShoppingSuggestionService : IShoppingSuggestionService
    {
        private readonly ApplicationDbContext _db;
        private readonly IConfiguration _config;
        private readonly ILogger<ShoppingSuggestionService> _logger;

        public ShoppingSuggestionService(
            ApplicationDbContext db,
            IConfiguration config,
            ILogger<ShoppingSuggestionService> logger)
        {
            _db = db;
            _config = config;
            _logger = logger;
        }

        public async Task<ShoppingPlanDto> GenerateWeekPlanAsync(string userId, List<UserFoodItemDto> currentInHouse)
        {
            if (string.IsNullOrWhiteSpace(userId))
                throw new InvalidOperationException("You must be signed in to generate a shopping plan.");

            currentInHouse ??= new List<UserFoodItemDto>();

            var profile = await _db.UsersInformation
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.UserId == userId);

            if (profile == null)
                throw new InvalidOperationException("Profile not found. Please complete your profile before generating a shopping plan.");

            var weekTargets = BuildWeekTargets(profile);
            var prompt = BuildOpenAiPrompt(profile, currentInHouse, weekTargets);

            var apiKey = _config["OpenAI:ApiKey"] ?? Environment.GetEnvironmentVariable("OPENAI_API_KEY");
            if (string.IsNullOrWhiteSpace(apiKey))
                throw new InvalidOperationException("OPENAI_API_KEY is missing. Set it in your environment variables and restart the app.");

            var model = _config["OpenAI:ShoppingModel"] ?? "gpt-5.1";
            var client = new ChatClient(model: model, apiKey: apiKey);

            var messages = new List<ChatMessage>
            {
                new SystemChatMessage(
                    "You are a nutrition and meal planning assistant. " +
                    "Return only valid JSON matching the required schema. " +
                    "Do not include markdown fences, explanations, or extra text."),
                new UserChatMessage(prompt)
            };

            var options = new ChatCompletionOptions
            {
                ResponseFormat = ChatResponseFormat.CreateJsonSchemaFormat(
                    jsonSchemaFormatName: "shopping_plan",
                    jsonSchema: BinaryData.FromBytes("""
                    {
                      "type": "object",
                      "properties": {
                        "shoppingList": {
                          "type": "array",
                          "items": {
                            "type": "object",
                            "properties": {
                              "name": { "type": "string" },
                              "quantity": { "type": "number" },
                              "unit": { "type": "string" }
                            },
                            "required": ["name", "quantity", "unit"],
                            "additionalProperties": false
                          }
                        },
                        "weekMealsSummary": {
                          "type": "array",
                          "items": {
                            "type": "object",
                            "properties": {
                              "day": { "type": "string" },
                              "title": { "type": "string" },
                              "calories": { "type": "integer" },
                              "proteinGrams": { "type": "integer" },
                              "carbsGrams": { "type": "integer" },
                              "fatGrams": { "type": "integer" }
                            },
                            "required": ["day", "title", "calories", "proteinGrams", "carbsGrams", "fatGrams"],
                            "additionalProperties": false
                          }
                        }
                      },
                      "required": ["shoppingList", "weekMealsSummary"],
                      "additionalProperties": false
                    }
                    """u8.ToArray()),
                    jsonSchemaIsStrict: true)
            };

            ChatCompletion completion = await client.CompleteChatAsync(messages, options);

            if (completion.Content == null || completion.Content.Count == 0)
                throw new InvalidOperationException("OpenAI returned an empty response.");

            var json = completion.Content[0].Text;
            if (string.IsNullOrWhiteSpace(json))
                throw new InvalidOperationException("OpenAI returned an empty response.");

            try
            {
                var plan = JsonSerializer.Deserialize<ShoppingPlanDto>(
                    json,
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                if (plan == null)
                    throw new InvalidOperationException("OpenAI returned an empty shopping plan.");

                plan.ShoppingList ??= new List<ShoppingLineDto>();
                plan.WeekMealsSummary ??= new List<WeeklyMealDto>();

                foreach (var meal in plan.WeekMealsSummary)
                {
                    var targetDay = weekTargets.FirstOrDefault(x =>
                        string.Equals(x.Day, meal.Day, StringComparison.OrdinalIgnoreCase));

                    if (targetDay != null)
                    {
                        meal.IsGymDay = targetDay.IsGymDay;
                    }
                }

                return plan;
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Failed to deserialize shopping plan JSON from OpenAI. Raw JSON: {Json}", json);
                throw new InvalidOperationException("OpenAI returned invalid JSON for the shopping plan.", ex);
            }
        }

        private static List<DailyTargetDto> BuildWeekTargets(UsersInformation profile)
        {
            var days = new[]
            {
                DayOfWeek.Monday,
                DayOfWeek.Tuesday,
                DayOfWeek.Wednesday,
                DayOfWeek.Thursday,
                DayOfWeek.Friday,
                DayOfWeek.Saturday,
                DayOfWeek.Sunday
            };

            return days.Select(day =>
            {
                var isGymDay = IsGymDay(profile.ChosenGymDays, day);

                return new DailyTargetDto
                {
                    Day = day.ToString(),
                    IsGymDay = isGymDay,
                    Calories = isGymDay ? profile.CaloriesTargetGymDay : profile.CaloriesTargetNonGymDay,
                    Protein = isGymDay ? profile.ProteinTargetGymDay : profile.ProteinTargetNonGymDay,
                    Carbs = isGymDay ? profile.CarbsTargetGymDay : profile.CarbsTargetNonGymDay,
                    Fat = isGymDay ? profile.FatTargetGymDay : profile.FatTargetNonGymDay,
                    Fiber = isGymDay ? profile.FiberTargetGymDay : profile.FiberTargetNonGymDay
                };
            }).ToList();
        }

        private static bool IsGymDay(GymDays chosenGymDays, DayOfWeek dayOfWeek)
        {
            var dayFlag = dayOfWeek switch
            {
                DayOfWeek.Monday => GymDays.Monday,
                DayOfWeek.Tuesday => GymDays.Tuesday,
                DayOfWeek.Wednesday => GymDays.Wednesday,
                DayOfWeek.Thursday => GymDays.Thursday,
                DayOfWeek.Friday => GymDays.Friday,
                DayOfWeek.Saturday => GymDays.Saturday,
                DayOfWeek.Sunday => GymDays.Sunday,
                _ => GymDays.None
            };

            return dayFlag != GymDays.None && (chosenGymDays & dayFlag) != 0;
        }

        private static string BuildOpenAiPrompt(
            UsersInformation profile,
            List<UserFoodItemDto> pantryItems,
            List<DailyTargetDto> weekTargets)
        {
            var sb = new StringBuilder();

            sb.AppendLine("Create a 7-day meal plan and shopping list.");
            sb.AppendLine("Use pantry items first whenever possible.");
            sb.AppendLine("Only add missing items to the shopping list.");
            sb.AppendLine("Meals should support the user's fitness goals and daily macro targets.");
            sb.AppendLine("Return only valid JSON.");
            sb.AppendLine();

            sb.AppendLine("USER PROFILE");
            sb.AppendLine($"- Age: {profile.Age}");
            sb.AppendLine($"- Sex: {profile.Gender}");
            sb.AppendLine($"- WeightKg: {profile.WeightInKg}");
            sb.AppendLine($"- HeightCm: {profile.HeightInCM}");
            sb.AppendLine($"- FitnessGoal: {profile.ChosenFitnessGoal}");
            sb.AppendLine($"- DailyFitnessLevel: {profile.EveryDayFitnessLevel}");
            sb.AppendLine($"- GymDays: {FormatGymDays(profile.ChosenGymDays)}");
            sb.AppendLine();

            sb.AppendLine("WEEK TARGETS");
            foreach (var day in weekTargets)
            {
                sb.AppendLine(
                    $"- {day.Day}: GymDay={day.IsGymDay}, Calories={day.Calories}, Protein={day.Protein}g, Carbs={day.Carbs}g, Fat={day.Fat}g, Fiber={day.Fiber}g");
            }

            sb.AppendLine();
            sb.AppendLine("CURRENT PANTRY ITEMS");

            if (pantryItems.Count == 0)
            {
                sb.AppendLine("- None");
            }
            else
            {
                foreach (var item in pantryItems)
                {
                    sb.AppendLine(
                        $"- Name={item.Name}, Quantity={item.Quantity}, Unit={item.Unit}, CaloriesPer100g={item.CaloriesPer100g?.ToString() ?? "unknown"}, ProteinPer100g={item.ProteinPer100g?.ToString() ?? "unknown"}, CarbsPer100g={item.CarbsPer100g?.ToString() ?? "unknown"}, FatPer100g={item.FatPer100g?.ToString() ?? "unknown"}, FiberPer100g={item.FiberPer100g?.ToString() ?? "unknown"}");
                }
            }

            sb.AppendLine();
            sb.AppendLine("RULES");
            sb.AppendLine("- Create breakfast, lunch, dinner, and optional snack ideas for each day.");
            sb.AppendLine("- Prefer pantry items before adding new ingredients.");
            sb.AppendLine("- Shopping list must only contain ingredients missing from the pantry.");
            sb.AppendLine("- Keep meals realistic and simple.");
            sb.AppendLine("- Try to keep each day close to the target macros.");
            sb.AppendLine("- Use kilograms, grams, ml, or item counts where suitable.");
            sb.AppendLine();

            sb.AppendLine("RETURN JSON IN THIS SHAPE");
            sb.AppendLine("""
            {
              "shoppingList": [
                { "name": "Chicken breast", "quantity": 1, "unit": "kg" }
              ],
              "weekMealsSummary": [
                {
                  "day": "Monday",
                  "title": "Breakfast: ..., Lunch: ..., Dinner: ...",
                  "calories": 2200,
                  "proteinGrams": 180,
                  "carbsGrams": 210,
                  "fatGrams": 65
                }
              ]
            }
            """);

            return sb.ToString();
        }

        private static string FormatGymDays(GymDays gymDays)
        {
            var selected = new List<string>();

            if ((gymDays & GymDays.Monday) != 0) selected.Add("Monday");
            if ((gymDays & GymDays.Tuesday) != 0) selected.Add("Tuesday");
            if ((gymDays & GymDays.Wednesday) != 0) selected.Add("Wednesday");
            if ((gymDays & GymDays.Thursday) != 0) selected.Add("Thursday");
            if ((gymDays & GymDays.Friday) != 0) selected.Add("Friday");
            if ((gymDays & GymDays.Saturday) != 0) selected.Add("Saturday");
            if ((gymDays & GymDays.Sunday) != 0) selected.Add("Sunday");

            return selected.Count == 0 ? "None" : string.Join(", ", selected);
        }

        private sealed class DailyTargetDto
        {
            public string Day { get; set; } = "";
            public bool IsGymDay { get; set; }
            public int Calories { get; set; }
            public int Protein { get; set; }
            public int Carbs { get; set; }
            public int Fat { get; set; }
            public int Fiber { get; set; }
        }
    }
}