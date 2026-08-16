using InTakeWise.Dto;
using InTakeWise.Helper;
using InTakeWise.Models;

namespace InTakeWise.ViewModels
{
    public class LogEntryViewModel
    {
        public string? UserName { get; set; }
        public LogType.LoggingType Mode { get; set; }
        public MealType SelectedTimeOfDay { get; set; }

        public string UserInput { get; set; } = "";
        public string? Error { get; set; }

        public MealLogEntry? GeneratedMealLog { get; set; }
        public WorkoutLogEntry? GeneratedWorkoutLog { get; set; }

        public DailyLogSummaryDto? DailySummary { get; set; }
    }
}
