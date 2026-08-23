using InTakeWise.Services;

namespace InTakeWise.Tests.Unit;

public sealed class AiLogResponseParserTests
{
    private readonly AiLogResponseParser _sut = new();

    [Fact]
    public void ParseMeal_AcceptsCompleteCaseInsensitiveJson()
    {
        const string json = """
            {
              "SUMMARY": " Chicken and rice ",
              "CALORIES": 650,
              "PROTEIN": 52,
              "CARBS": 70,
              "FAT": 18,
              "FIBER": 8
            }
            """;

        var result = _sut.ParseMeal(json);

        Assert.Equal("Chicken and rice", result.Summary);
        Assert.Equal(650, result.Calories);
        Assert.Equal(52, result.Protein);
    }

    [Theory]
    [InlineData("")]
    [InlineData("{not-json")]
    [InlineData("{\"summary\":\"Incomplete\",\"calories\":500}")]
    public void ParseMeal_RejectsEmptyMalformedOrIncompleteJson(string json)
    {
        Assert.Throws<InvalidOperationException>(() => _sut.ParseMeal(json));
    }

    [Theory]
    [InlineData("{\"summary\":\"Bad\",\"calories\":-1,\"protein\":20,\"carbs\":20,\"fat\":10,\"fiber\":5}")]
    [InlineData("{\"summary\":\"Bad\",\"calories\":500,\"protein\":1001,\"carbs\":20,\"fat\":10,\"fiber\":5}")]
    [InlineData("{\"summary\":\"Bad\",\"calories\":500,\"protein\":20,\"carbs\":20,\"fat\":10,\"fiber\":251}")]
    public void ParseMeal_RejectsImplausibleNutrition(string json)
    {
        Assert.Throws<InvalidOperationException>(() => _sut.ParseMeal(json));
    }

    [Fact]
    public void ParseWorkout_AcceptsCompleteJson()
    {
        const string json = """
            {
              "activityType": " Strength training ",
              "durationMinutes": 60,
              "intensity": " Moderate ",
              "caloriesBurned": 420
            }
            """;

        var result = _sut.ParseWorkout(json);

        Assert.Equal("Strength training", result.ActivityType);
        Assert.Equal(60, result.DurationMinutes);
        Assert.Equal("Moderate", result.Intensity);
        Assert.Equal(420, result.CaloriesBurned);
    }

    [Theory]
    [InlineData("{\"activityType\":\"Run\",\"durationMinutes\":0,\"intensity\":\"High\",\"caloriesBurned\":100}")]
    [InlineData("{\"activityType\":\"Run\",\"durationMinutes\":481,\"intensity\":\"High\",\"caloriesBurned\":100}")]
    [InlineData("{\"activityType\":\"\",\"durationMinutes\":30,\"intensity\":\"High\",\"caloriesBurned\":100}")]
    [InlineData("{\"activityType\":\"Run\",\"durationMinutes\":30,\"intensity\":\"High\",\"caloriesBurned\":5001}")]
    public void ParseWorkout_RejectsMissingOrImplausibleValues(string json)
    {
        Assert.Throws<InvalidOperationException>(() => _sut.ParseWorkout(json));
    }

    [Theory]
    [InlineData("meal")]
    [InlineData("workout")]
    public void Parse_RejectsUnknownJsonMembers(string responseType)
    {
        if (responseType == "meal")
        {
            const string mealJson =
                "{\"summary\":\"Meal\",\"calories\":500,\"protein\":20," +
                "\"carbs\":20,\"fat\":10,\"fiber\":5,\"extra\":true}";

            Assert.Throws<InvalidOperationException>(() => _sut.ParseMeal(mealJson));
            return;
        }

        const string workoutJson =
            "{\"activityType\":\"Run\",\"durationMinutes\":30," +
            "\"intensity\":\"High\",\"caloriesBurned\":300,\"extra\":true}";

        Assert.Throws<InvalidOperationException>(() => _sut.ParseWorkout(workoutJson));
    }
}

