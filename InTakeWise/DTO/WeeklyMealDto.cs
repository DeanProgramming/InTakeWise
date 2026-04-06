using System.ComponentModel.DataAnnotations;

namespace InTakeWise.Dto
{
    public class WeeklyMealDto
    {

        public string Day { get; set; } = "";
        public bool IsGymDay { get; set; }
        public string Title { get; set; } = "";
        public int Calories { get; set; }
        public int ProteinGrams { get; set; }
        public int CarbsGrams { get; set; }
        public int FatGrams { get; set; }
    }
}