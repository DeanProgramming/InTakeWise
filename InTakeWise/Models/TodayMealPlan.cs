using System.ComponentModel.DataAnnotations;

namespace InTakeWise.Models
{
    public class TodayMealPlan
    {
        public MealSuggestion Breakfast { get; set; } = new();
        public MealSuggestion Dinner { get; set; } = new();
        public MealSuggestion Tea { get; set; } = new();
    }
}