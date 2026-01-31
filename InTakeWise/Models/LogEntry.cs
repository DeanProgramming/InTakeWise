namespace InTakeWise.Models
{
    public class LogEntry
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string UserId { get; set; }
        public LogType.LoggingType Type { get; set; }
        public DateTime Timestamp { get; set; }
         

        public int? Calories { get; set; }

        public int? Protein { get; set; }
        public int? Carbs { get; set; }
        public int? Fat { get; set; }
        public int? Fiber { get; set; }
    }
}