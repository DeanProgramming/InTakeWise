using InTakeWise.Models;
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
        public string Name { get; set; } = "";

        [Range(0.01, double.MaxValue, ErrorMessage = "Amount is required.")]
        public decimal? Quantity { get; set; }

        [Required(ErrorMessage = "Unit is required.")]
        public string Unit { get; set; } = "";
    }
}