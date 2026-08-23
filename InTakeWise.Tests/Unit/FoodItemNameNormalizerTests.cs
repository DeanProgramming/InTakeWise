using InTakeWise.Services;
using Xunit;

namespace InTakeWise.Tests.Unit;

public sealed class FoodItemNameNormalizerTests
{
    [Theory]
    [InlineData("  Chicken Breast  ", "chicken breast")]
    [InlineData("GREEK YOGHURT", "greek yoghurt")]
    [InlineData("red  pepper", "red  pepper")]
    public void NormalizeName_TrimsAndUsesInvariantLowercase(
        string input,
        string expected)
    {
        var result = FoodItemNameNormalizer.NormalizeName(input);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void NormalizeName_ReturnsEmptyForNull()
    {
        var result = FoodItemNameNormalizer.NormalizeName(null);

        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void CleanDisplayName_TrimsWithoutChangingCasing()
    {
        var result = FoodItemNameNormalizer.CleanDisplayName("  Greek Yoghurt ");

        Assert.Equal("Greek Yoghurt", result);
    }

}
