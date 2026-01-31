using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace InTakeWise.Models
{
    public class HomeViewModel
    {
        public string? UserName { get; set; }
         
        public LogEntry? GeneratedLog { get; set; } 
        public string? UserInput { get; set; }
        public LogType.LoggingType Mode { get; set; }
    }
}
