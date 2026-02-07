using static InTakeWise.Models.LogType;

namespace InTakeWise.Models
{
    public abstract class LogEntryBase
    {
        public int Id { get; set; }
        public string UserId { get; set; } = default!;
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public string? RawInput { get; set; }
    }

    public class MealLogEntry : LogEntryBase
    {
        public int? Calories { get; set; }
        public int? Protein { get; set; }
        public int? Carbs { get; set; }
        public int? Fat { get; set; }
        public int? Fiber { get; set; }
    }

    public class WorkoutLogEntry : LogEntryBase
    {
        public int? CaloriesBurned { get; set; }
    }
}