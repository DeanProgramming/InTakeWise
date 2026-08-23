using InTakeWise.Services;

namespace InTakeWise.Tests.Unit;

public sealed class AiInputValidatorTests
{
    [Theory]
    [InlineData(AiOperation.MealAnalysis, "  chicken and rice  ", "chicken and rice")]
    [InlineData(AiOperation.WorkoutAnalysis, "  45 minute run  ", "45 minute run")]
    public void NormalizeLogInput_TrimsSupportedInput(
        AiOperation operation,
        string input,
        string expected)
    {
        var result = AiInputValidator.NormalizeLogInput(input, operation);

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void NormalizeLogInput_RejectsMissingMealInput(string? input)
    {
        var exception = Assert.Throws<AiInputValidationException>(() =>
            AiInputValidator.NormalizeLogInput(input, AiOperation.MealAnalysis));

        Assert.Equal("Meal input cannot be empty.", exception.UserMessage);
    }

    [Fact]
    public void NormalizeLogInput_RejectsInputAboveMaximumLength()
    {
        var input = new string(
            'x',
            AiInputValidator.MaximumLogInputCharacters + 1);

        var exception = Assert.Throws<AiInputValidationException>(() =>
            AiInputValidator.NormalizeLogInput(
                input,
                AiOperation.WorkoutAnalysis));

        Assert.Contains("2,000 characters or fewer", exception.UserMessage);
    }

}
