using System.ComponentModel.DataAnnotations;

namespace InTakeWise.Models
{
    public class FoodItem
    {
        public int Id { get; set; }

        [Required, MaxLength(100)]
        public string Name { get; set; } = "";

        [Required, MaxLength(100)]
        public string NormalizedName { get; set; } = "";

        public int? CaloriesPer100g { get; set; }
        public int? ProteinPer100g { get; set; }
        public int? CarbsPer100g { get; set; }
        public int? FatPer100g { get; set; }
        public int? FiberPer100g { get; set; }
    }
}