using System.ComponentModel.DataAnnotations;

namespace InTakeWise.Models
{
    public class PantryItem
    {
        public int Id { get; set; }

        public string UserId { get; set; } = default!;
        public int FoodItemId { get; set; }

        public decimal Quantity { get; set; }
        public string Unit { get; set; } = "";
        public DateTime? ExpiryDate { get; set; }

        public FoodItem FoodItem { get; set; } = default!;
    }

}