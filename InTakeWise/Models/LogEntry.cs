using System.ComponentModel.DataAnnotations;
using InTakeWise.Helper;
using InTakeWise.Validation;
using Microsoft.AspNetCore.Identity;
using static InTakeWise.Models.LogType;

namespace InTakeWise.Models
{
    public abstract class LogEntryBase
    {
        public int Id { get; set; }
        [Required, MaxLength(450)]
        public string UserId { get; set; } = default!;
        public IdentityUser User { get; set; } = default!;

        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        [MaxLength(ValidationLimits.MaximumAiLogInputCharacters)]
        public string? RawInput { get; set; }
    }

    public class MealLogEntry : LogEntryBase
    {
        public MealType TimeEat { get; set; }
        public DateTime LogDateLocal { get; set; }

        [Range(0, int.MaxValue)]
        public int? Calories { get; set; }
        [Range(0, int.MaxValue)]
        public int? Protein { get; set; }
        [Range(0, int.MaxValue)]
        public int? Carbs { get; set; }
        [Range(0, int.MaxValue)]
        public int? Fat { get; set; }
        [Range(0, int.MaxValue)]
        public int? Fiber { get; set; }
    }

    public class WorkoutLogEntry : LogEntryBase
    {
        [MaxLength(ValidationLimits.MaximumWorkoutActivityCharacters)]
        public string? ActivityType { get; set; }

        [Range(0, int.MaxValue)]
        public int? DurationMinutes { get; set; }

        [MaxLength(ValidationLimits.MaximumWorkoutIntensityCharacters)]
        public string? Intensity { get; set; }

        [Range(0, int.MaxValue)]
        public int? CaloriesBurned { get; set; }
    }
}
