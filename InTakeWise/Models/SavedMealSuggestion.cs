using System.ComponentModel.DataAnnotations;

namespace InTakeWise.Models
{
    public enum MealType
    {
        Breakfast = 1,
        Dinner = 2,
        Tea = 3
    }

    public class SavedTodayMealPlan
    {
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = "";

        public DateOnly PlanDate { get; set; }

        public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

        public List<SavedMeal> Meals { get; set; } = new();
    }

    public class SavedMeal
    {
        public int Id { get; set; }

        public int SavedTodayMealPlanId { get; set; }
        public SavedTodayMealPlan SavedTodayMealPlan { get; set; } = null!;

        public MealType MealType { get; set; }

        [Required]
        public string Title { get; set; } = "";

        public string Description { get; set; } = "";

        public int Calories { get; set; }
        public int ProteinGrams { get; set; }
        public int CarbsGrams { get; set; }
        public int FatGrams { get; set; }

        public List<SavedMealIngredient> Ingredients { get; set; } = new();
        public List<SavedMealStep> Steps { get; set; } = new();
    }

    public class SavedMealIngredient
    {
        public int Id { get; set; }

        public int SavedMealId { get; set; }
        public SavedMeal SavedMeal { get; set; } = null!;

        [Required]
        public string Value { get; set; } = "";

        public int SortOrder { get; set; }
    }

    public class SavedMealStep
    {
        public int Id { get; set; }

        public int SavedMealId { get; set; }
        public SavedMeal SavedMeal { get; set; } = null!;

        [Required]
        public string Value { get; set; } = "";

        public int SortOrder { get; set; }
    }
}