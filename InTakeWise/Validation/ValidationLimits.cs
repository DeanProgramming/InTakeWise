namespace InTakeWise.Validation;

public static class ValidationLimits
{
    public const int MinimumProfileNameCharacters = 2;
    public const int MaximumProfileNameCharacters = 25;

    public const int MinimumProfileAge = 13;
    public const int MaximumProfileAge = 120;
    public const int MinimumHeightCentimetres = 120;
    public const int MaximumHeightCentimetres = 250;
    public const int MinimumWeightKilograms = 30;
    public const int MaximumWeightKilograms = 350;

    public const int MaximumFoodNameCharacters = 100;
    public const int MaximumPantryUnitCharacters = 30;
    public const int MaximumAiLogInputCharacters = 2_000;
    public const int MaximumWorkoutActivityCharacters = 100;
    public const int MaximumWorkoutIntensityCharacters = 50;
    public const int MaximumShoppingItemNameCharacters = 120;
    public const int MaximumShoppingDayCharacters = 20;
    public const int MaximumShoppingTitleCharacters = 1_500;

    public const decimal MaximumPantryQuantity = 100_000m;
}
