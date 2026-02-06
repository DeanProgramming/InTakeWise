using System.ComponentModel.DataAnnotations;

namespace InTakeWise.Dto
{
    public class ShoppingLineDto
    {
        public string Name { get; set; } = "";
        public decimal Quantity { get; set; }
        public string Unit { get; set; } = "";
    }
}