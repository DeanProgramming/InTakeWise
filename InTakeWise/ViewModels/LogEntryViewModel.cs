using InTakeWise.Models;
using static InTakeWise.Models.LogType;

namespace InTakeWise.ViewModels
{
    public class LogEntryViewModel
    {
        public string? UserName { get; set; }
        public LoggingType Mode { get; set; }
        public string? UserInput { get; set; }

        public MealLogEntry? GeneratedMealLog { get; set; }
        public WorkoutLogEntry? GeneratedWorkoutLog { get; set; }


        public TimeOfDay SelectedTimeOfDay { get; set; }
    }
}
