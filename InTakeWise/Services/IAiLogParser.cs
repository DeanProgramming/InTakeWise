using InTakeWise.Dto;
using InTakeWise.Models;

namespace InTakeWise.Services
{
    public interface IAiLogParser
    {
        Task<MealAnalysisDto> AnalyzeMealAsync(string userInput);
        Task<WorkoutAnalysisDto> AnalyzeWorkoutAsync(string userInput, UsersInformation? profile);
    }
}