namespace InTakeWise.Services
{
    using InTakeWise.Models;
    using InTakeWise.Services;

    public class LogEntryService : ILogEntryService
    {
        public Task<LogEntry> LogMealInfoAsync(string userId, string userInput)
        {
            var mealInfo = MealProcessor.MealProcessed(userInput);

            var log = new LogEntry
            {
                Id = Guid.NewGuid().ToString(),
                UserId = userId,
                Type = LogType.LoggingType.Meal,
                Timestamp = DateTime.UtcNow,
                Calories = mealInfo.Calories,
                Protein = mealInfo.Protein,
                Carbs = mealInfo.Carbs,
                Fat = mealInfo.Fat,
                Fiber = mealInfo.Fiber
            };

            return Task.FromResult(log);
        }
        public Task<LogEntry> LogWorkoutInfoAsync(string userId, string userInput)
        {
            var WorkInfo = WorkoutProcessor.WorkoutProcessed(userInput);

            var log = new LogEntry
            {
                Id = Guid.NewGuid().ToString(),
                UserId = userId,
                Type = LogType.LoggingType.Workout,
                Timestamp = DateTime.UtcNow,
                Calories = WorkInfo
            };

            return Task.FromResult(log);
        }
    }
}