using InTakeWise.Models;

namespace InTakeWise.ViewModels
{
    public class MealSuggestionViewModel
    {
        public TodayMealPlan? Plan { get; set; }

        public string SelectedMeal { get; set; } = "Breakfast";

        public MealSuggestion? SelectedMealSuggestion =>
            Plan is null ? null :
            SelectedMeal switch
            {
                "Lunch" => Plan.Lunch,
                "Dinner" => Plan.Dinner,
                "Snack" => Plan.Snack,
                _ => Plan.Breakfast
            };

        public string StatusMessage { get; set; } = "";
    }
}