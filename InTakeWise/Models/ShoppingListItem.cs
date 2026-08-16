using System.ComponentModel.DataAnnotations;
using InTakeWise.Validation;

namespace InTakeWise.Models
{
    public class ShoppingListItem
    {
        public int Id { get; set; }

        public int ShoppingListId { get; set; }
        public ShoppingList ShoppingList { get; set; } = default!;

        public int? FoodItemId { get; set; }
        public FoodItem? FoodItem { get; set; }

        [Required, MaxLength(ValidationLimits.MaximumShoppingItemNameCharacters)]
        public string Name { get; set; } = "";

        [Range(typeof(decimal), "0.01", "100000")]
        public decimal Quantity { get; set; }

        [Required, MaxLength(ValidationLimits.MaximumPantryUnitCharacters)]
        public string Unit { get; set; } = "";
    }
}
