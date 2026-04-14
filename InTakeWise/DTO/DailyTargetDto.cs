using System.ComponentModel.DataAnnotations;

namespace InTakeWise.Dto
{
    public sealed class DailyTargetDto
    {
        public string Day { get; set; } = "";
        public DateTime DateLocal { get; set; }
        public bool IsGymDay { get; set; }
        public int Calories { get; set; }
        public int Protein { get; set; }
        public int Carbs { get; set; }
        public int Fat { get; set; }
        public int Fiber { get; set; }
    } 
}