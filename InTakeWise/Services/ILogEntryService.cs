using InTakeWise.Models;
namespace InTakeWise.Services
{
    public interface ILogEntryService
    {
        Task<LogEntry> LogMealInfoAsync(string userId, string userInput);

        Task<LogEntry> LogWorkoutInfoAsync(string id, string userInput);
    } 
}