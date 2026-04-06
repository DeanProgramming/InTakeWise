namespace InTakeWise.Models
{
    public class UserFoodItemDto
    {
        public int PantryItemId { get; set; }
        public int FoodItemId { get; set; }

        public string Name { get; set; } = "";
        public decimal Quantity { get; set; }
        public string Unit { get; set; } = "";

        public int? CaloriesPer100g { get; set; }
        public int? ProteinPer100g { get; set; }
        public int? CarbsPer100g { get; set; }
        public int? FatPer100g { get; set; }
        public int? FiberPer100g { get; set; }
    }
}