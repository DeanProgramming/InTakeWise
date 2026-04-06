using InTakeWise.Dto;

namespace InTakeWise.ViewModels
{
    public class ShoppingSuggestionViewModel
    {
        public List<ShoppingLineDto> ShoppingList { get; set; } = new();
        public List<WeeklyMealDto> WeekMealsSummary { get; set; } = new();
        public string? Error { get; set; }
    }
}