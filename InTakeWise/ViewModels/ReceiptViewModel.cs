namespace InTakeWise.ViewModels
{
    public class ReceiptViewModel
    {
        public string? ReturnUrl { get; set; }
        public List<PantryItemInputViewModel> Items { get; set; } = new();
    }
}