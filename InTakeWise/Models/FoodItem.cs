using System.ComponentModel.DataAnnotations;

namespace InTakeWise.Models
{
    public class FoodItem
    {
        public string Name { get; set; } = "";
        public decimal Quantity { get; set; }        
        public string Unit { get; set; } = "";     
        public DateTime? ExpiryDate { get; set; }       
    }
}