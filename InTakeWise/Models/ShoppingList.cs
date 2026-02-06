using System.ComponentModel.DataAnnotations;

namespace InTakeWise.Models
{
    public class ShoppingList
    {
        public int Id { get; set; }
        public string UserId { get; set; } = default!;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public List<ShoppingListItem> Items { get; set; } = new();
    }
}