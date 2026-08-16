using System.ComponentModel.DataAnnotations;
using InTakeWise.Validation;

namespace InTakeWise.Models
{
    public class FoodItem
    {
        public int Id { get; set; }

        [Required, MaxLength(ValidationLimits.MaximumFoodNameCharacters)]
        public string Name { get; set; } = "";

        [Required, MaxLength(ValidationLimits.MaximumFoodNameCharacters)]
        public string NormalizedName { get; set; } = "";

        public int? CaloriesPer100g { get; set; }
        public int? ProteinPer100g { get; set; }
        public int? CarbsPer100g { get; set; }
        public int? FatPer100g { get; set; }
        public int? FiberPer100g { get; set; }
    }
}
