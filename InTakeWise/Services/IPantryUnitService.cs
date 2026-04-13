using InTakeWise.ViewModels;

namespace InTakeWise.Services
{
    public interface IPantryUnitService
    {
        PantryMergeResult MergeRowsByFoodName(IEnumerable<PantryItemInputViewModel> rows);

        PantryQuantityMergeResult MergeWithExisting(
            string itemName,
            decimal incomingQuantity,
            string incomingUnit,
            IEnumerable<PantryQuantityItem> existingItems);
    }
}