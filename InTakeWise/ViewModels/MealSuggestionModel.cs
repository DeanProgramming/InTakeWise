using InTakeWise.Helper;
using InTakeWise.Models;

namespace InTakeWise.ViewModels
{
    public class MealSuggestionViewModel
    {
        public TodayMealPlan? Plan { get; set; }

        public MealType SelectedMeal { get; set; } = MealType.Breakfast;

        public MealSuggestion? SelectedMealSuggestion =>
            Plan is null ? null :
            SelectedMeal switch
            {
                MealType.Dinner => Plan.Dinner,
                MealType.Tea => Plan.Tea,
                MealType.Snack => Plan.Snack,
                _ => Plan.Breakfast
            };

        public string StatusMessage { get; set; } = "";
    }
}