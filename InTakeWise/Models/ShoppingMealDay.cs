using System.ComponentModel.DataAnnotations;

namespace InTakeWise.Models
{
    public class ShoppingMealDay
    {
        public int Id { get; set; }

        public int ShoppingListId { get; set; }
        public ShoppingList ShoppingList { get; set; } = default!;

        [Required]
        public string Day { get; set; } = "";

        [Required]
        public string Title { get; set; } = "";

        public string MealDetailsJson { get; set; } = "";

        public int Calories { get; set; }
        public int ProteinGrams { get; set; }
        public int CarbsGrams { get; set; }
        public int FatGrams { get; set; }
        public bool IsGymDay { get; set; }
    }
}