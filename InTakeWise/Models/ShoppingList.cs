using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace InTakeWise.Models
{
    public class ShoppingList
    {
        public int Id { get; set; }

        [Required, MaxLength(450)]
        public string UserId { get; set; } = default!;
        public IdentityUser User { get; set; } = default!;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime WeekStartLocalDate { get; set; }

        public List<ShoppingListItem> Items { get; set; } = new();
        public List<ShoppingMealDay> Meals { get; set; } = new();
    }
}
