namespace InTakeWise.ViewModels
{ 
    public class ShoppingSuggestionViewModel
    {
        public List<ShoppingLineVm> ShoppingList { get; set; } = new();
        public List<WeeklyMealVm> WeekMealsSummary { get; set; } = new();
    }

    public class ShoppingLineVm
    {
        public string Name { get; set; } = "";
        public decimal Quantity { get; set; }
        public string Unit { get; set; } = "";
    }

    public class WeeklyMealVm
    {
        public string Day { get; set; } = "";
        public string Title { get; set; } = "";

        public int Calories { get; set; }
        public int ProteinGrams { get; set; }
        public int CarbsGrams { get; set; }
        public int FatGrams { get; set; }
    }
}
