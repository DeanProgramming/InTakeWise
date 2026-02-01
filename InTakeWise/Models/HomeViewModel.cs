using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace InTakeWise.Models
{
    public class HomeViewModel
    {
        public string? UserName { get; set; } 

        public LogType.LoggingType Mode { get; set; } 

        public MealPlanSectionViewModel LogInfo { get; set; } = new();

        public MacroSummaryViewModel MacroSummary { get; set; } = new();

        public MealPlanViewModel MealPlan { get; set; } = new();

        public ShoppingSuggestionViewModel ShoppingSuggestionPlan { get; set; } = new(); 
        public ItemsInPantryViewModel ItemsInPantry { get; set; } = new(); 
    }

    public class ShoppingSuggestionViewModel
    {
        public List<FoodItem> ShoppingList { get; set; }
        public List<ShoppingMealsWeekSuggestion> WeekMealsSummary { get; set; }
    }

    public class MealPlanSectionViewModel
    {
        public string? WorkoutOrEatenMealUserInput { get; set; }       
        public LogEntry? GeneratedLog { get; set; }   
    } 

    public class MacroSummaryViewModel
    {
        public int CaloriesConsumed { get; set; }
        public int CaloriesTarget { get; set; }

        public int Protein { get; set; }
        public int ProteinTarget { get; set; }

        public int Carbs { get; set; }
        public int CarbsTarget { get; set; }

        public int Fat { get; set; }
        public int FatTarget { get; set; }

        public int Fiber { get; set; }
        public int FiberTarget { get; set; }
    }

    public class MealPlanViewModel
    {
        public List<FoodItem> ItemsAtHome { get; set; } = new();
        public MealSuggestion? SuggestedMeal { get; set; }

        public string StatusMessage { get; set; } = "";
    }

    public class ItemsInPantryViewModel
    {
        public List<FoodItem> ItemsAtHome { get; set; } = new();
    }
}
