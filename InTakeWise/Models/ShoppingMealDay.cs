using System.ComponentModel.DataAnnotations;
using InTakeWise.Validation;

namespace InTakeWise.Models
{
    public class ShoppingMealDay
    {
        public int Id { get; set; }

        public int ShoppingListId { get; set; }
        public ShoppingList ShoppingList { get; set; } = default!;

        [Required, MaxLength(ValidationLimits.MaximumShoppingDayCharacters)]
        public string Day { get; set; } = "";

        [Required, MaxLength(ValidationLimits.MaximumShoppingTitleCharacters)]
        public string Title { get; set; } = "";

        public DateTime MealDateLocal { get; set; }
        [Required]
        public string MealDetailsJson { get; set; } = "";

        [Range(0, int.MaxValue)]
        public int Calories { get; set; }
        [Range(0, int.MaxValue)]
        public int ProteinGrams { get; set; }
        [Range(0, int.MaxValue)]
        public int CarbsGrams { get; set; }
        [Range(0, int.MaxValue)]
        public int FatGrams { get; set; }
        public bool IsGymDay { get; set; }
    }
}
