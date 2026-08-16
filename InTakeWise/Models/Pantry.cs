using System.ComponentModel.DataAnnotations;
using InTakeWise.Validation;
using Microsoft.AspNetCore.Identity;

namespace InTakeWise.Models
{
    public class PantryItem
    {
        public int Id { get; set; }

        [Required, MaxLength(450)]
        public string UserId { get; set; } = default!;
        public IdentityUser User { get; set; } = default!;

        public int FoodItemId { get; set; }

        [Range(typeof(decimal), "0", "100000")]
        public decimal Quantity { get; set; }

        [Required, MaxLength(ValidationLimits.MaximumPantryUnitCharacters)]
        public string Unit { get; set; } = "";
        public DateTime? ExpiryDate { get; set; }

        public FoodItem FoodItem { get; set; } = default!;
    }

}
