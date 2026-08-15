using InTakeWise.Services;
using InTakeWise.ViewModels;

namespace InTakeWise.Tests.Unit;

public sealed class PantryUnitServiceTests
{
    private readonly PantryUnitService _sut = new();

    [Fact]
    public void MergeRowsByFoodName_CombinesMassAliasesAndPromotesToKilograms()
    {
        var result = _sut.MergeRowsByFoodName(
        [
            Row("Chicken", 500m, "grams"),
            Row("chicken", 0.75m, "kilogram")
        ]);

        var row = Assert.Single(result.Rows);
        Assert.False(result.HasErrors);
        Assert.Equal(1.25m, row.Quantity);
        Assert.Equal("kg", row.Unit);
    }

    [Fact]
    public void MergeRowsByFoodName_PromotesMilligramsToGrams()
    {
        var result = _sut.MergeRowsByFoodName(
        [
            Row("Salt", 250m, "mg"),
            Row("Salt", 750m, "milligrams")
        ]);

        var row = Assert.Single(result.Rows);
        Assert.Equal(1m, row.Quantity);
        Assert.Equal("g", row.Unit);
    }

    [Fact]
    public void MergeWithExisting_CombinesVolumeAndPromotesToLitres()
    {
        var result = _sut.MergeWithExisting(
            "Milk",
            250m,
            "ml",
            [new PantryQuantityItem { Quantity = 1m, Unit = "litre" }]);

        Assert.False(result.HasErrors);
        Assert.Equal(1.25m, result.Quantity);
        Assert.Equal("l", result.Unit);
    }

    [Fact]
    public void MergeRowsByFoodName_ReturnsErrorForIncompatibleFamilies()
    {
        var result = _sut.MergeRowsByFoodName(
        [
            Row("Tomatoes", 400m, "g"),
            Row("tomatoes", 2m, "tins")
        ]);

        Assert.True(result.HasErrors);
        Assert.Empty(result.Rows);
        Assert.Contains("incompatible units", Assert.Single(result.Errors));
    }

    [Fact]
    public void MergeRowsByFoodName_GroupsCaseInsensitivelyAndIgnoresBlankNames()
    {
        var result = _sut.MergeRowsByFoodName(
        [
            Row("  Oats  ", 1m, "pack"),
            Row("oats", 2m, "packs"),
            Row("   ", 99m, "packs")
        ]);

        var row = Assert.Single(result.Rows);
        Assert.Equal("Oats", row.Name);
        Assert.Equal(3m, row.Quantity);
        Assert.Equal("packs", row.Unit);
    }

    private static PantryItemInputViewModel Row(string name, decimal quantity, string unit) => new()
    {
        Name = name,
        Quantity = quantity,
        Unit = unit
    };
}
