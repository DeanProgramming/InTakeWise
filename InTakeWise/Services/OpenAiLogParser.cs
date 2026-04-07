using System.Text.Json;
using InTakeWise.Dto;
using InTakeWise.Models;
using Microsoft.Extensions.Logging;
using OpenAI.Chat;

namespace InTakeWise.Services
{
    public sealed class OpenAiLogParser : IAiLogParser
    {
        private readonly ChatClient _chatClient;
        private readonly ILogger<OpenAiLogParser> _logger;

        public OpenAiLogParser(
            ChatClient chatClient,
            ILogger<OpenAiLogParser> logger)
        {
            _chatClient = chatClient;
            _logger = logger;
        }

        public async Task<MealAnalysisDto> AnalyzeMealAsync(string userInput)
        {
            if (string.IsNullOrWhiteSpace(userInput))
                throw new InvalidOperationException("Meal input cannot be empty.");

            var messages = new List<ChatMessage>
            {
                new SystemChatMessage(
                    "You estimate nutrition from plain-language meal descriptions. " +
                    "Return only valid JSON matching the schema. " +
                    "Be realistic and conservative when portions are unclear."),
                new UserChatMessage($"""
                    Estimate total nutrition for this meal log.

                    User input:
                    {userInput}

                    Rules:
                    - Infer sensible household portions if omitted.
                    - Sum all foods into total calories, protein, carbs, fat, and fiber.
                    - Use integers.
                    - If uncertain, choose the most realistic conservative estimate.
                    - Return only JSON.
                    """)
            };

            var options = new ChatCompletionOptions
            {
                ResponseFormat = ChatResponseFormat.CreateJsonSchemaFormat(
                    jsonSchemaFormatName: "meal_analysis",
                    jsonSchema: BinaryData.FromBytes("""
                    {
                      "type": "object",
                      "properties": {
                        "summary": { "type": "string" },
                        "calories": { "type": "integer" },
                        "protein": { "type": "integer" },
                        "carbs": { "type": "integer" },
                        "fat": { "type": "integer" },
                        "fiber": { "type": "integer" }
                      },
                      "required": ["summary", "calories", "protein", "carbs", "fat", "fiber"],
                      "additionalProperties": false
                    }
                    """u8.ToArray()),
                    jsonSchemaIsStrict: true)
            };

            ChatCompletion completion = await _chatClient.CompleteChatAsync(messages, options);

            if (completion.Content == null || completion.Content.Count == 0)
                throw new InvalidOperationException("AI returned an empty meal response.");

            var json = string.Concat(completion.Content.Select(x => x.Text));

            if (string.IsNullOrWhiteSpace(json))
                throw new InvalidOperationException("AI returned an empty meal JSON response.");

            try
            {
                return JsonSerializer.Deserialize<MealAnalysisDto>(
                    json,
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    })
                    ?? throw new InvalidOperationException("Meal analysis came back empty.");
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Meal analysis JSON parse failed. Raw: {Json}", json);
                throw new InvalidOperationException("AI returned invalid meal JSON.", ex);
            }
        }

        public async Task<WorkoutAnalysisDto> AnalyzeWorkoutAsync(string userInput, UsersInformation? profile)
        {
            if (string.IsNullOrWhiteSpace(userInput))
                throw new InvalidOperationException("Workout input cannot be empty.");

            var weight = profile?.WeightInKg ?? 75;
            var age = profile?.Age ?? 30;
            var sex = profile?.Gender.ToString() ?? "unknown";

            var messages = new List<ChatMessage>
            {
                new SystemChatMessage(
                    "You estimate workout details and calories burned from plain-language workout descriptions. " +
                    "Return only valid JSON matching the schema. " +
                    "Use the user profile when relevant and be conservative."),
                new UserChatMessage($"""
                    Estimate the workout and calories burned.

                    User profile:
                    - WeightKg: {weight}
                    - Age: {age}
                    - Sex: {sex}

                    Workout input:
                    {userInput}

                    Rules:
                    - Infer duration only if the user strongly implies it.
                    - Infer intensity only if reasonable.
                    - Return a conservative calorie estimate.
                    - Use integers.
                    - Return only JSON.
                    """)
            };

            var options = new ChatCompletionOptions
            {
                ResponseFormat = ChatResponseFormat.CreateJsonSchemaFormat(
                    jsonSchemaFormatName: "workout_analysis",
                    jsonSchema: BinaryData.FromBytes("""
                    {
                      "type": "object",
                      "properties": {
                        "activityType": { "type": "string" },
                        "durationMinutes": { "type": "integer" },
                        "intensity": { "type": "string" },
                        "caloriesBurned": { "type": "integer" }
                      },
                      "required": ["activityType", "durationMinutes", "intensity", "caloriesBurned"],
                      "additionalProperties": false
                    }
                    """u8.ToArray()),
                    jsonSchemaIsStrict: true)
            };

            ChatCompletion completion = await _chatClient.CompleteChatAsync(messages, options);

            if (completion.Content == null || completion.Content.Count == 0)
                throw new InvalidOperationException("AI returned an empty workout response.");

            var json = string.Concat(completion.Content.Select(x => x.Text));

            if (string.IsNullOrWhiteSpace(json))
                throw new InvalidOperationException("AI returned an empty workout JSON response.");

            try
            {
                return JsonSerializer.Deserialize<WorkoutAnalysisDto>(
                    json,
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    })
                    ?? throw new InvalidOperationException("Workout analysis came back empty.");
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Workout analysis JSON parse failed. Raw: {Json}", json);
                throw new InvalidOperationException("AI returned invalid workout JSON.", ex);
            }
        }
    }
}