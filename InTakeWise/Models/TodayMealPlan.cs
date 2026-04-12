namespace InTakeWise.Models
{
    public class TodayMealPlan
    {
        public string Day { get; set; } = "";
        public bool IsGymDay { get; set; }

        public int Calories { get; set; }
        public int ProteinGrams { get; set; }
        public int CarbsGrams { get; set; }
        public int FatGrams { get; set; }

        public MealSuggestion? Breakfast { get; set; }
        public MealSuggestion? Lunch { get; set; }
        public MealSuggestion? Dinner { get; set; }
        public MealSuggestion? Snack { get; set; }
    }
}