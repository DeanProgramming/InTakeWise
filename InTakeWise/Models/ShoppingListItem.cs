using System.ComponentModel.DataAnnotations;

namespace InTakeWise.Models
{
    public class ShoppingListItem
    {
        public int Id { get; set; }

        public int ShoppingListId { get; set; }
        public ShoppingList ShoppingList { get; set; } = default!;

        public int FoodItemId { get; set; }
        public FoodItem FoodItem { get; set; } = default!;

        public decimal Quantity { get; set; }
        public string Unit { get; set; } = "";
    }

}