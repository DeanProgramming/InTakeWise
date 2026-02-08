
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
                "Dinner" => Plan.Dinner,
                "Tea" => Plan.Tea,
                _ => Plan.Breakfast
            };

        public string StatusMessage { get; set; } = "";
    }
}