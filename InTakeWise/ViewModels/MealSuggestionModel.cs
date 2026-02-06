
using InTakeWise.Models;

namespace InTakeWise.ViewModels
{
    public class MealSuggestionViewModel
    {
        public MealSuggestion? SuggestedMeal { get; set; }
        public string StatusMessage { get; set; } = "";
    }
}
