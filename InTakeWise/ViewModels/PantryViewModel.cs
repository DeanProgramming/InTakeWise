using InTakeWise.Models;
using InTakeWise.Validation;
using System.ComponentModel.DataAnnotations;

namespace InTakeWise.ViewModels
{
    public class PantryViewModel
    {
        public string? ReturnUrl { get; set; }
        public List<PantryItemInputViewModel> Items { get; set; } = new();
    }

    public class PantryItemInputViewModel
    {
        public int Id { get; set; }
        public int FoodItemId { get; set; }

        [Required(ErrorMessage = "Food is required.")]
        [StringLength(
            ValidationLimits.MaximumFoodNameCharacters,
            ErrorMessage = "Food must be {1} characters or fewer.")]
        public string Name { get; set; } = "";

        [Required(ErrorMessage = "Amount is required.")]
        [Range(
            typeof(decimal),
            "0.01",
            "100000",
            ErrorMessage = "Amount must be between {1} and {2}.")]
        public decimal? Quantity { get; set; }

        [Required(ErrorMessage = "Unit is required.")]
        [StringLength(
            ValidationLimits.MaximumPantryUnitCharacters,
            ErrorMessage = "Unit must be {1} characters or fewer.")]
        public string Unit { get; set; } = "";
    }
}
