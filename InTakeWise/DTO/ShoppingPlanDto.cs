using System.ComponentModel.DataAnnotations;

namespace InTakeWise.Dto
{
    public class ShoppingPlanDto
    {
        public List<ShoppingLineDto> ShoppingList { get; set; } = new();
        public List<WeeklyMealDto> WeekMealsSummary { get; set; } = new();
    }

}