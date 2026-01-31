using System.ComponentModel.DataAnnotations;

namespace InTakeWise.Models
{
    public class LogType
    {
        public enum LoggingType
        {
            Meal,
            Workout
        }

        public class LogEntryInputVm
        {
            public LogType Type { get; set; }

            [Required]
            [MinLength(3)]
            public string Text { get; set; } = "";
        }

    }
}