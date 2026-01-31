using InTakeWise.Models;
namespace InTakeWise.Services
{
    public interface ILogEntryService
    {
        Task<LogEntry> CreateMealAsync(string userId, string userInput);

        Task<LogEntry> CreateWorkoutAsync(string id, string userInput);
    } 
}