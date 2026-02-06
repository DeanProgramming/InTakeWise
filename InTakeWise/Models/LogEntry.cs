using static InTakeWise.Models.LogType;

namespace InTakeWise.Models
{
    public class LogEntry
    {
        public int Id { get; set; }

        public string UserId { get; set; } = default!;
        public LoggingType Type { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        public int? Calories { get; set; }
        public int? Protein { get; set; }
        public int? Carbs { get; set; }
        public int? Fat { get; set; }
        public int? Fiber { get; set; }
    }

}