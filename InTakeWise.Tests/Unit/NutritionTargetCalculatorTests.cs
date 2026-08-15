using InTakeWise.Models;
using InTakeWise.Services;
using InTakeWise.ViewModels;

namespace InTakeWise.Tests.Unit;

public sealed class NutritionTargetCalculatorTests
{
    private readonly NutritionTargetCalculator _sut = new();

    [Theory]
    [InlineData(FitnessGoal.HeavyCut)]
    [InlineData(FitnessGoal.LightCut)]
    [InlineData(FitnessGoal.Maintain)]
    [InlineData(FitnessGoal.LightBulk)]
    [InlineData(FitnessGoal.HeavyBulk)]
    public void CalculateTargets_ReturnsCoherentMacrosForEveryGoal(FitnessGoal goal)
    {
        var targets = _sut.CalculateTargets(StandardProfile(), StandardActivity(), goal);

        AssertCoherent(targets.GymDay, minimumCalories: 1500);
        AssertCoherent(targets.NonGymDay, minimumCalories: 1500);
        Assert.InRange(targets.GymDay.Protein, 90, 220);
        Assert.InRange(targets.GymDay.Fiber, 25, 45);
    }

    [Fact]
    public void CalculateTargets_IncreasesWeeklyCaloriesAcrossOrderedGoals()
    {
        var profile = StandardProfile();
        var activity = StandardActivity();
        var orderedGoals = new[]
        {
            FitnessGoal.HeavyCut,
            FitnessGoal.LightCut,
            FitnessGoal.Maintain,
            FitnessGoal.LightBulk,
            FitnessGoal.HeavyBulk
        };

        var weeklyCalories = orderedGoals
            .Select(goal => WeeklyCalories(_sut.CalculateTargets(profile, activity, goal), gymDays: 3))
            .ToArray();

        Assert.True(weeklyCalories.SequenceEqual(weeklyCalories.OrderBy(x => x)));
        Assert.Equal(weeklyCalories.Length, weeklyCalories.Distinct().Count());
    }

    [Theory]
    [InlineData(GymDays.None)]
    [InlineData(GymDays.Monday | GymDays.Tuesday | GymDays.Wednesday | GymDays.Thursday | GymDays.Friday | GymDays.Saturday | GymDays.Sunday)]
    public void CalculateTargets_UsesOneDailyTargetAtZeroOrSevenGymDays(GymDays gymDays)
    {
        var activity = StandardActivity();
        activity.ChosenGymDays = gymDays;

        var targets = _sut.CalculateTargets(StandardProfile(), activity, FitnessGoal.Maintain);

        Assert.Equal(targets.GymDay.Calories, targets.NonGymDay.Calories);
    }

    [Fact]
    public void CalculateTargets_RespectsFemaleCalorieFloorOnAggressiveCut()
    {
        var profile = new ProfileStep1ViewModel
        {
            ProfileUserName = "Boundary",
            Age = 80,
            Gender = Genders.Female,
            HeightInCM = 150,
            WeightInKg = 45
        };
        var activity = new ProfileStep2ViewModel
        {
            EveryDayFitnessLevel = FitnessLevel.Low,
            ChosenGymDays = GymDays.None
        };

        var targets = _sut.CalculateTargets(profile, activity, FitnessGoal.HeavyCut);

        Assert.Equal(1200, targets.GymDay.Calories);
        Assert.Equal(1200, targets.NonGymDay.Calories);
    }

    [Theory]
    [InlineData(40, FitnessGoal.HeavyBulk, 90)]
    [InlineData(200, FitnessGoal.HeavyCut, 220)]
    public void CalculateTargets_ClampsProteinAtSafetyBoundaries(
        int weightKg,
        FitnessGoal goal,
        int expectedProtein)
    {
        var profile = StandardProfile();
        profile.WeightInKg = weightKg;

        var targets = _sut.CalculateTargets(profile, StandardActivity(), goal);

        Assert.Equal(expectedProtein, targets.GymDay.Protein);
        Assert.Equal(expectedProtein, targets.NonGymDay.Protein);
    }

    [Fact]
    public void Predictions_KeepWeightDirectionAndBodyFatWithinBounds()
    {
        var profile = StandardProfile();
        var activity = StandardActivity();
        var startBodyFat = _sut.EstimateBodyFatPercent(profile);

        var cutWeight = _sut.PredictWeight(profile, activity, 3, FitnessGoal.LightCut);
        var bulkWeight = _sut.PredictWeight(profile, activity, 3, FitnessGoal.LightBulk);
        var cutBodyFat = _sut.PredictBodyFatPercent(
            profile,
            activity,
            3,
            FitnessGoal.LightCut,
            startBodyFat);

        Assert.True(cutWeight < profile.WeightInKg);
        Assert.True(bulkWeight > profile.WeightInKg);
        Assert.InRange(cutBodyFat, 4.0, 60.0);
    }

    private static void AssertCoherent(DailyNutritionTargets target, int minimumCalories)
    {
        Assert.True(target.Calories >= minimumCalories);
        Assert.True(target.Protein >= 0);
        Assert.True(target.Carbs >= 0);
        Assert.True(target.Fat >= 0);

        var macroCalories = target.Protein * 4 + target.Carbs * 4 + target.Fat * 9;
        Assert.InRange(Math.Abs(macroCalories - target.Calories), 0, 2);
    }

    private static int WeeklyCalories(NutritionTargets targets, int gymDays) =>
        targets.GymDay.Calories * gymDays + targets.NonGymDay.Calories * (7 - gymDays);

    private static ProfileStep1ViewModel StandardProfile() => new()
    {
        ProfileUserName = "Dean",
        Age = 26,
        Gender = Genders.Male,
        HeightInCM = 190,
        WeightInKg = 86
    };

    private static ProfileStep2ViewModel StandardActivity() => new()
    {
        EveryDayFitnessLevel = FitnessLevel.Medium,
        ChosenGymDays = GymDays.Monday | GymDays.Wednesday | GymDays.Friday
    };
}
