namespace InTakeWise.Services
{
    using InTakeWise.Data;
    using InTakeWise.Models;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.IdentityModel.Abstractions;

    public class LogEntryService : ILogEntryService
    {
        private readonly ApplicationDbContext _db;

        public LogEntryService(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<MealLogEntry> LogMealInfoAsync(string userId, string userInput, TimeOfDay logTime)
        {
            var mealInfo = MealProcessor.MealProcessed(userInput);

            var log = new MealLogEntry
            {
                UserId = userId,
                TimeEat = logTime,
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

        public async Task<MealLogEntry?> GetTodayMealAsync(string userId, TimeOfDay timeOfDay)
        {
            var tz = TimeZoneInfo.FindSystemTimeZoneById("Europe/London");

            var nowLocal = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tz);
            var startLocal = nowLocal.Date;
            var endLocal = startLocal.AddDays(1);

            var startUtc = TimeZoneInfo.ConvertTimeToUtc(startLocal, tz);
            var endUtc = TimeZoneInfo.ConvertTimeToUtc(endLocal, tz);

            return await _db.MealLogs.AsNoTracking()
                .Where(x => x.UserId == userId
                            && x.TimeEat == timeOfDay
                            && x.Timestamp >= startUtc
                            && x.Timestamp < endUtc)
                .OrderByDescending(x => x.Timestamp)
                .FirstOrDefaultAsync();
        }

        public async Task<bool> GetCompletedTodayMealAsync(string userId, TimeOfDay timeOfDay)
        {
            MealLogEntry? meal = await GetTodayMealAsync(userId, timeOfDay);

            if (meal == null)
            {
                return false;
            }
             
            return true;
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