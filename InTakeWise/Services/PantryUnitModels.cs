using InTakeWise.ViewModels;

namespace InTakeWise.Services
{
    public sealed class PantryMergeResult
    {
        public List<PantryItemInputViewModel> Rows { get; init; } = new();
        public List<string> Errors { get; init; } = new();
        public bool HasErrors => Errors.Count > 0;
    }

    public sealed class PantryQuantityMergeResult
    {
        public decimal Quantity { get; init; }
        public string Unit { get; init; } = "";
        public List<string> Errors { get; init; } = new();
        public bool HasErrors => Errors.Count > 0;
    }

    public sealed class PantryQuantityItem
    {
        public decimal Quantity { get; set; }
        public string Unit { get; set; } = "";
    }
}