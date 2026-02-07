namespace InTakeWise.Services
{
    using InTakeWise.Data;
    using InTakeWise.Models;
    using Microsoft.EntityFrameworkCore;

    public class LogEntryService : ILogEntryService
    {
        private readonly ApplicationDbContext _db;

        public LogEntryService(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<MealLogEntry> LogMealInfoAsync(string userId, string userInput)
        {
            var mealInfo = MealProcessor.MealProcessed(userInput);

            var log = new MealLogEntry
            {
                UserId = userId,
                Timestamp = DateTime.UtcNow,
                RawInput = userInput,
                Calories = mealInfo.Calories,
                Protein = mealInfo.Protein,
                Carbs = mealInfo.Carbs,
                Fat = mealInfo.Fat,
                Fiber = mealInfo.Fiber
            };

            _db.MealLogs.Add(log);
            await _db.SaveChangesAsync();
            return log;
        }

        public async Task<WorkoutLogEntry> LogWorkoutInfoAsync(string userId, string userInput)
        {
            var workInfo = WorkoutProcessor.WorkoutProcessed(userInput);

            var log = new WorkoutLogEntry
            {
                UserId = userId,
                Timestamp = DateTime.UtcNow,
                RawInput = userInput,
                CaloriesBurned = workInfo
            };

            _db.WorkoutLogs.Add(log);
            await _db.SaveChangesAsync();
            return log;
        }

        public Task<MealLogEntry?> GetMealByIdAsync(int id, string userId) =>
            _db.MealLogs.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId);

        public Task<WorkoutLogEntry?> GetWorkoutByIdAsync(int id, string userId) =>
            _db.WorkoutLogs.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId);
    }
}