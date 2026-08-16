using InTakeWise.Dto;
using InTakeWise.Models;

namespace InTakeWise.Services
{
    public interface IAiLogParser
    {
        Task<MealAnalysisDto> AnalyzeMealAsync(string userId, string userInput, CancellationToken cancellationToken = default);

        Task<WorkoutAnalysisDto> AnalyzeWorkoutAsync(string userId, string userInput, UsersInformation? profile, CancellationToken cancellationToken = default);
    }
}
