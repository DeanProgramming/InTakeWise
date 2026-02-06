using InTakeWise.Models;

namespace InTakeWise.ViewModels
{
    public class LogEntryViewModel
    {
        public string? UserName { get; set; }

        public LogType.LoggingType Mode { get; set; }

        public string UserInput { get; set; } = "";

        public LogEntry? GeneratedLog { get; set; }
    }
}
