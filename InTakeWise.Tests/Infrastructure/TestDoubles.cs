using InTakeWise.Dto;
using InTakeWise.Models;
using InTakeWise.Security;
using InTakeWise.Services;

namespace InTakeWise.Tests.Infrastructure;

internal sealed class StubAiLogParser : IAiLogParser
{
    public MealAnalysisDto MealResult { get; set; } = new()
    {
        Summary = "Test meal",
        Calories = 600,
        Protein = 45,
        Carbs = 70,
        Fat = 18,
        Fiber = 9
    };

    public WorkoutAnalysisDto WorkoutResult { get; set; } = new()
    {
        ActivityType = "Strength training",
        DurationMinutes = 60,
        Intensity = "Moderate",
        CaloriesBurned = 400
    };

    public Task<MealAnalysisDto> AnalyzeMealAsync(string userId, string userInput) =>
        Task.FromResult(MealResult);

    public Task<MealAnalysisDto> AnalyzeMealAsync(string userId, string userInput, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<WorkoutAnalysisDto> AnalyzeWorkoutAsync(
        string userId,
        string userInput,
        UsersInformation? profile) => Task.FromResult(WorkoutResult);

    public Task<WorkoutAnalysisDto> AnalyzeWorkoutAsync(string userId, string userInput, UsersInformation? profile, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}

internal sealed class TestAppClock : IAppClock
{
    public TestAppClock(
        DateTime? utcNow = null,
        DateTime? londonNow = null,
        DateTime? startUtc = null,
        DateTime? endUtc = null)
    {
        UtcNow = utcNow ?? new DateTime(2026, 8, 15, 12, 0, 0, DateTimeKind.Utc);
        LondonNow = londonNow ?? new DateTime(2026, 8, 15, 13, 0, 0);
        StartUtc = startUtc ?? new DateTime(2026, 8, 14, 23, 0, 0, DateTimeKind.Utc);
        EndUtc = endUtc ?? new DateTime(2026, 8, 15, 23, 0, 0, DateTimeKind.Utc);
    }

    public DateTime UtcNow { get; }
    public DateTime LondonNow { get; }
    public DayOfWeek LondonDayOfWeek => LondonNow.DayOfWeek;
    public DateTime StartUtc { get; }
    public DateTime EndUtc { get; }

    public (DateTime StartUtc, DateTime EndUtc) GetTodayLondonRangeUtc() =>
        (StartUtc, EndUtc);
}

internal sealed class AllowAllDemoAiGuard : IDemoAiGuard
{
    public Task EnsureLiveAiAllowedAsync(string userId) => Task.CompletedTask;
}
