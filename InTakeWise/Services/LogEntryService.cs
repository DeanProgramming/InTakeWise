using InTakeWise.Data;
using InTakeWise.Dto;
using InTakeWise.Helper;
using InTakeWise.Models;
using Microsoft.EntityFrameworkCore;

namespace InTakeWise.Services
{
    public class LogEntryService : ILogEntryService
    {
        private readonly ApplicationDbContext _db;
        private readonly IAiLogParser _aiLogParser;
        private readonly IAppClock _clock;

        public LogEntryService(ApplicationDbContext db, IAiLogParser aiLogParser, IAppClock clock)
        {
            _db = db;
            _aiLogParser = aiLogParser;
            _clock = clock;
        }

        public async Task<MealLogEntry> LogMealInfoAsync(
            string userId,
            string userInput,
            MealType logTime,
            CancellationToken cancellationToken = default)
        {
            if (!Enum.IsDefined(logTime))
            {
                throw new AiInputValidationException(
                    "Select a valid meal time.");
            }

            var normalizedInput = AiInputValidator.NormalizeLogInput(
                userInput,
                AiOperation.MealAnalysis);

            var mealInfo = await _aiLogParser.AnalyzeMealAsync(
                userId,
                normalizedInput,
                cancellationToken);

            var logDateLocal = _clock.LondonNow.Date;

            var oldLog = await _db.MealLogs
                .Where(x => x.UserId == userId
                         && x.TimeEat == logTime
                         && x.LogDateLocal == logDateLocal)
                .OrderByDescending(x => x.Timestamp)
                .FirstOrDefaultAsync(cancellationToken);

            if (oldLog != null)
            {
                _db.MealLogs.Remove(oldLog);
            }

            var log = new MealLogEntry
            {
                UserId = userId,
                TimeEat = logTime,
                LogDateLocal = logDateLocal,
                Timestamp = _clock.UtcNow,
                RawInput = normalizedInput,
                Calories = mealInfo.Calories,
                Protein = mealInfo.Protein,
                Carbs = mealInfo.Carbs,
                Fat = mealInfo.Fat,
                Fiber = mealInfo.Fiber
            };

            _db.MealLogs.Add(log);
            await _db.SaveChangesAsync(cancellationToken);
            return log;
        }

        public async Task<MealLogEntry?> GetTodayMealAsync(string userId, MealType timeOfDay)
        {
            var todayLocal = _clock.LondonNow.Date;

            return await _db.MealLogs.AsNoTracking()
                .Where(x => x.UserId == userId
                            && x.TimeEat == timeOfDay
                            && x.LogDateLocal == todayLocal)
                .OrderByDescending(x => x.Timestamp)
                .FirstOrDefaultAsync();
        }

        public async Task<bool> GetCompletedTodayMealAsync(string userId, MealType timeOfDay)
        {
            MealLogEntry? meal = await GetTodayMealAsync(userId, timeOfDay);
            return meal != null;
        }

        public async Task<WorkoutLogEntry> LogWorkoutInfoAsync(
            string userId,
            string userInput,
            CancellationToken cancellationToken = default)
        {
            var normalizedInput = AiInputValidator.NormalizeLogInput(
                userInput,
                AiOperation.WorkoutAnalysis);

            var profile = await _db.UsersInformation
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    x => x.UserId == userId,
                    cancellationToken);

            var workInfo = await _aiLogParser.AnalyzeWorkoutAsync(
                userId,
                normalizedInput,
                profile,
                cancellationToken);

            var log = new WorkoutLogEntry
            {
                UserId = userId,
                Timestamp = _clock.UtcNow,
                RawInput = normalizedInput,
                CaloriesBurned = workInfo.CaloriesBurned,
                ActivityType = workInfo.ActivityType,
                DurationMinutes = workInfo.DurationMinutes,
                Intensity = workInfo.Intensity
            };

            _db.WorkoutLogs.Add(log);
            await _db.SaveChangesAsync(cancellationToken);
            return log;
        }

        public async Task<DailyLogSummaryDto?> GetTodaySummaryAsync(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
                return null;

            var profile = await _db.UsersInformation
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.UserId == userId);

            if (profile == null)
                return null;

            var (startUtc, endUtc) = _clock.GetTodayLondonRangeUtc();
            var logDateLocal = _clock.LondonNow.Date;
            var todayLocal = _clock.LondonDayOfWeek;

            var meals = await _db.MealLogs
                .AsNoTracking()
                .Where(x => x.UserId == userId
                            && x.LogDateLocal == logDateLocal)
                .ToListAsync();

            var workouts = await _db.WorkoutLogs
                .AsNoTracking()
                .Where(x => x.UserId == userId
                            && x.Timestamp >= startUtc
                            && x.Timestamp < endUtc)
                .ToListAsync();

            var isGymDay = IsGymDay(profile.ChosenGymDays, todayLocal);

            return new DailyLogSummaryDto
            {
                IsGymDay = isGymDay,

                CaloriesTarget = isGymDay ? profile.CaloriesTargetGymDay : profile.CaloriesTargetNonGymDay,
                ProteinTarget = isGymDay ? profile.ProteinTargetGymDay : profile.ProteinTargetNonGymDay,
                CarbsTarget = isGymDay ? profile.CarbsTargetGymDay : profile.CarbsTargetNonGymDay,
                FatTarget = isGymDay ? profile.FatTargetGymDay : profile.FatTargetNonGymDay,
                FiberTarget = isGymDay ? profile.FiberTargetGymDay : profile.FiberTargetNonGymDay,

                CaloriesEaten = meals.Sum(x => x.Calories ?? 0),
                ProteinEaten = meals.Sum(x => x.Protein ?? 0),
                CarbsEaten = meals.Sum(x => x.Carbs ?? 0),
                FatEaten = meals.Sum(x => x.Fat ?? 0),
                FiberEaten = meals.Sum(x => x.Fiber ?? 0),

                CaloriesBurned = workouts.Sum(x => x.CaloriesBurned ?? 0)
            };
        }

        public Task<WorkoutLogEntry?> GetLatestWorkoutAsync(string userId) => _db.WorkoutLogs
                                                                                .AsNoTracking()
                                                                                .Where(x => x.UserId == userId)
                                                                                .OrderByDescending(x => x.Timestamp)
                                                                                .FirstOrDefaultAsync();

        public Task<MealLogEntry?> GetMealByIdAsync(int id, string userId) =>
            _db.MealLogs.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId);

        public Task<WorkoutLogEntry?> GetWorkoutByIdAsync(int id, string userId) =>
            _db.WorkoutLogs.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId);

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
    }
}
