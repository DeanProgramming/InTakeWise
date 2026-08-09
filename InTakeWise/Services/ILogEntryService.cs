using InTakeWise.Dto;
using InTakeWise.Helper;
using InTakeWise.Models;
namespace InTakeWise.Services
{
    public interface ILogEntryService
    {
        Task<MealLogEntry?> GetTodayMealAsync(string userId, MealType timeOfDay);
        Task<bool> GetCompletedTodayMealAsync(string userId, MealType timeOfDay);

        Task<MealLogEntry> LogMealInfoAsync(string userId, string userInput, MealType logTime);
        Task<WorkoutLogEntry> LogWorkoutInfoAsync(string userId, string userInput);

        Task<MealLogEntry?> GetMealByIdAsync(int id, string userId);
        Task<WorkoutLogEntry?> GetWorkoutByIdAsync(int id, string userId);
        Task<DailyLogSummaryDto?> GetTodaySummaryAsync(string userId);
        Task<WorkoutLogEntry?> GetLatestWorkoutAsync(string userId);
    } 
}