using System.ComponentModel.DataAnnotations;

namespace InTakeWise.Models
{
    public class MealSuggestion
    {
        public string Title { get; set; } = "";
        public string Description { get; set; } = "";
        public int Calories { get; set; }
        public int ProteinGrams { get; set; }
        public int CarbsGrams { get; set; }
        public int FatGrams { get; set; }

        public List<string> IngredientsUsed { get; set; } = new();
        public List<string> Steps { get; set; } = new();
    }
}