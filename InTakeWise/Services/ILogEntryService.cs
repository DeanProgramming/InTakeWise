using InTakeWise.Models;
namespace InTakeWise.Services
{
    public interface ILogEntryService
    {
        Task<MealLogEntry> LogMealInfoAsync(string userId, string userInput);
        Task<WorkoutLogEntry> LogWorkoutInfoAsync(string userId, string userInput);

        Task<MealLogEntry?> GetMealByIdAsync(int id, string userId);
        Task<WorkoutLogEntry?> GetWorkoutByIdAsync(int id, string userId);
    } 
}