using System.Text.Json;
using InTakeWise.Dto;
using InTakeWise.Services;

namespace InTakeWise.Tests.Unit;

public sealed class ShoppingPlanResponseParserTests
{
    private readonly ShoppingPlanResponseParser _sut = new();

    [Fact]
    public void Parse_NormalizesContentAndUsesTrustedDayMetadata()
    {
        var expectedDays = ExpectedDays();
        var plan = ValidPlan();
        plan.ShoppingList[0].Name = "  Greek Yoghurt  ";
        plan.ShoppingList[0].Unit = " GRAMS ";
        plan.WeekMealsSummary.Reverse();
        plan.WeekMealsSummary[0].MealDateLocal = new DateTime(1999, 1, 1);
        plan.WeekMealsSummary[0].IsGymDay = false;
        plan.WeekMealsSummary[1].MealDateLocal = new DateTime(1999, 1, 2);
        plan.WeekMealsSummary[1].IsGymDay = true;

        var result = _sut.Parse(Json(plan), expectedDays);

        var shoppingLine = Assert.Single(result.ShoppingList);
        Assert.Equal("Greek Yoghurt", shoppingLine.Name);
        Assert.Equal("grams", shoppingLine.Unit);
        Assert.Collection(
            result.WeekMealsSummary,
            monday =>
            {
                Assert.Equal("Monday", monday.Day);
                Assert.Equal(new DateTime(2026, 8, 24), monday.MealDateLocal);
                Assert.True(monday.IsGymDay);
            },
            tuesday =>
            {
                Assert.Equal("Tuesday", tuesday.Day);
                Assert.Equal(new DateTime(2026, 8, 25), tuesday.MealDateLocal);
                Assert.False(tuesday.IsGymDay);
            });
    }

    [Theory]
    [InlineData("zero-quantity")]
    [InlineData("duplicate-line")]
    [InlineData("unsafe-unit")]
    public void Parse_RejectsInvalidShoppingData(string scenario)
    {
        var plan = ValidPlan();

        switch (scenario)
        {
            case "zero-quantity":
                plan.ShoppingList[0].Quantity = 0;
                break;
            case "duplicate-line":
                plan.ShoppingList.Add(new ShoppingLineDto
                {
                    Name = " greek yoghurt ",
                    Quantity = 2,
                    Unit = "G"
                });
                break;
            case "unsafe-unit":
                plan.ShoppingList[0].Unit = "<script>";
                break;
        }

        Assert.Throws<InvalidOperationException>(() =>
            _sut.Parse(Json(plan), ExpectedDays()));
    }

    [Theory]
    [InlineData("missing")]
    [InlineData("duplicate")]
    [InlineData("unexpected")]
    public void Parse_RejectsInvalidMealDayLayout(string scenario)
    {
        var plan = ValidPlan();

        switch (scenario)
        {
            case "missing":
                plan.WeekMealsSummary.RemoveAt(1);
                break;
            case "duplicate":
                plan.WeekMealsSummary[1].Day = "monday";
                break;
            case "unexpected":
                plan.WeekMealsSummary[1].Day = "Wednesday";
                break;
        }

        Assert.Throws<InvalidOperationException>(() =>
            _sut.Parse(Json(plan), ExpectedDays()));
    }

    [Theory]
    [InlineData("calories-high")]
    [InlineData("protein-negative")]
    public void Parse_RejectsImplausibleDailyNutrition(string scenario)
    {
        var plan = ValidPlan();
        var meal = plan.WeekMealsSummary[0];

        if (scenario == "calories-high")
        {
            meal.Calories = 10_001;
        }
        else
        {
            meal.ProteinGrams = -1;
        }

        Assert.Throws<InvalidOperationException>(() =>
            _sut.Parse(Json(plan), ExpectedDays()));
    }

    [Theory]
    [InlineData("overview")]
    [InlineData("ingredients")]
    [InlineData("steps")]
    public void Parse_RejectsIncompleteRequiredMealDetails(string missingPart)
    {
        var plan = ValidPlan();
        var breakfast = plan.WeekMealsSummary[0].MealDetails.Breakfast;

        switch (missingPart)
        {
            case "overview":
                breakfast.Overview = " ";
                break;
            case "ingredients":
                breakfast.Ingredients = [];
                break;
            case "steps":
                breakfast.Steps = [];
                break;
        }

        Assert.Throws<InvalidOperationException>(() =>
            _sut.Parse(Json(plan), ExpectedDays()));
    }

    private static IReadOnlyList<DailyTargetDto> ExpectedDays() =>
    [
        new DailyTargetDto
        {
            Day = "Monday",
            DateLocal = new DateTime(2026, 8, 24),
            IsGymDay = true,
            Calories = 2_500,
            Protein = 180,
            Carbs = 300,
            Fat = 70,
            Fiber = 35
        },
        new DailyTargetDto
        {
            Day = "Tuesday",
            DateLocal = new DateTime(2026, 8, 25),
            IsGymDay = false,
            Calories = 2_200,
            Protein = 180,
            Carbs = 240,
            Fat = 70,
            Fiber = 30
        }
    ];

    private static ShoppingPlanDto ValidPlan() => new()
    {
        ShoppingList =
        [
            new ShoppingLineDto
            {
                Name = "Greek Yoghurt",
                Quantity = 500,
                Unit = "g"
            }
        ],
        WeekMealsSummary =
        [
            Meal("Monday"),
            Meal("Tuesday")
        ]
    };

    private static WeeklyMealDto Meal(string day) => new()
    {
        Day = day,
        MealDateLocal = new DateTime(2000, 1, 1),
        IsGymDay = false,
        Title = $" {day} meal plan ",
        Calories = 2_200,
        ProteinGrams = 180,
        CarbsGrams = 240,
        FatGrams = 70,
        MealDetails = new DailyMealDetailsDto
        {
            Breakfast = RequiredDetail("Breakfast"),
            Lunch = RequiredDetail("Lunch"),
            Dinner = RequiredDetail("Dinner"),
            Snack = new MealDetailDto(),
            LateSnack = new MealDetailDto()
        }
    };

    private static MealDetailDto RequiredDetail(string name) => new()
    {
        Overview = $"{name} overview",
        Ingredients = [$"{name} ingredient"],
        Steps = [$"Prepare {name.ToLowerInvariant()}"]
    };

    private static string Json(ShoppingPlanDto plan) =>
        JsonSerializer.Serialize(plan);
}
