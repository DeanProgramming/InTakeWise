using InTakeWise.Models;
using InTakeWise.ViewModels;

namespace InTakeWise.Services;

public sealed record DailyNutritionTargets(
    int Calories,
    int Protein,
    int Carbs,
    int Fat,
    int Fiber);

public sealed record NutritionTargets(
    DailyNutritionTargets GymDay,
    DailyNutritionTargets NonGymDay);

public interface INutritionTargetCalculator
{
    NutritionTargets CalculateTargets(
        ProfileStep1ViewModel profile,
        ProfileStep2ViewModel activity,
        FitnessGoal goal);

    double PredictWeight(
        ProfileStep1ViewModel profile,
        ProfileStep2ViewModel activity,
        int months,
        FitnessGoal goal);

    double EstimateBodyFatPercent(ProfileStep1ViewModel profile);

    double PredictBodyFatPercent(
        ProfileStep1ViewModel profile,
        ProfileStep2ViewModel activity,
        int months,
        FitnessGoal goal,
        double startBodyFatPercent);
}

public sealed class NutritionTargetCalculator : INutritionTargetCalculator
{
    public NutritionTargets CalculateTargets(
        ProfileStep1ViewModel profile,
        ProfileStep2ViewModel activity,
        FitnessGoal goal)
    {
        ArgumentNullException.ThrowIfNull(profile);
        ArgumentNullException.ThrowIfNull(activity);

        var (gymCalories, nonGymCalories) = CalculateCalories(profile, activity, goal);
        var protein = ClampInt((int)Math.Round(profile.WeightInKg * ProteinPerKg(goal)), 90, 220);

        var fatBase = (int)Math.Round(profile.WeightInKg * FatPerKg(goal));
        var fatMinimum = Math.Max((int)Math.Round(profile.WeightInKg * 0.6), 40);
        var fatGym = Math.Max(fatBase, fatMinimum);
        var fatNonGym = Math.Max(fatBase, fatMinimum);

        var carbsGym = CarbsFrom(gymCalories, protein, fatGym);
        var carbsNonGym = CarbsFrom(nonGymCalories, protein, fatNonGym);

        if (carbsGym > 0)
        {
            var carbohydrateBump = (int)Math.Round(carbsGym * 0.15);
            var fatReduction = (int)Math.Round(carbohydrateBump * 4 / 9.0);

            fatGym = Math.Max(fatGym - fatReduction, fatMinimum);
            carbsGym = CarbsFrom(gymCalories, protein, fatGym);
        }

        return new NutritionTargets(
            new DailyNutritionTargets(
                gymCalories,
                protein,
                carbsGym,
                fatGym,
                FiberFromCalories(gymCalories)),
            new DailyNutritionTargets(
                nonGymCalories,
                protein,
                carbsNonGym,
                fatNonGym,
                FiberFromCalories(nonGymCalories)));
    }

    public double PredictWeight(
        ProfileStep1ViewModel profile,
        ProfileStep2ViewModel activity,
        int months,
        FitnessGoal goal)
    {
        ArgumentNullException.ThrowIfNull(profile);
        ArgumentNullException.ThrowIfNull(activity);

        var goalMonthlyDelta = GoalMonthlyDelta(goal);
        var fitnessMultiplier = FitnessMultiplier(activity.EveryDayFitnessLevel);
        var gymBonus = Math.Clamp(CountBits((int)activity.ChosenGymDays), 0, 7) * 0.1;
        var direction = Math.Sign(goalMonthlyDelta);
        var monthlyDelta = (goalMonthlyDelta * fitnessMultiplier) + (gymBonus * direction);
        var predicted = profile.WeightInKg + (monthlyDelta * months);

        return Math.Max(predicted, 0);
    }

    public double EstimateBodyFatPercent(ProfileStep1ViewModel profile)
    {
        ArgumentNullException.ThrowIfNull(profile);

        var heightMetres = profile.HeightInCM / 100.0;
        var bmi = profile.WeightInKg / (heightMetres * heightMetres);
        var sex = profile.Gender switch
        {
            Genders.Male => 1.0,
            Genders.Female => 0.0,
            _ => 0.5
        };

        var bodyFat = (1.20 * bmi) + (0.23 * profile.Age) - (10.8 * sex) - 5.4;
        return Math.Clamp(bodyFat, 2.0, 60.0);
    }

    public double PredictBodyFatPercent(
        ProfileStep1ViewModel profile,
        ProfileStep2ViewModel activity,
        int months,
        FitnessGoal goal,
        double startBodyFatPercent)
    {
        ArgumentNullException.ThrowIfNull(profile);
        ArgumentNullException.ThrowIfNull(activity);

        months = Math.Max(months, 0);

        var startWeight = (double)profile.WeightInKg;
        var startFatMass = startWeight * startBodyFatPercent / 100.0;
        var predictedWeight = PredictWeight(profile, activity, months, goal);

        if (predictedWeight <= 0.0)
        {
            return 0.0;
        }

        if (goal == FitnessGoal.Maintain)
        {
            var gymDays = CountBits((int)activity.ChosenGymDays);
            var recomp = gymDays >= 3 ? 0.20 : 0.0;
            var fatMass = Math.Max(startFatMass - (recomp * months), 0);
            var leanMass = (startWeight - startFatMass) + (recomp * months);

            return Math.Clamp(fatMass / (fatMass + leanMass) * 100.0, 2.0, 60.0);
        }

        var fatFraction = GetFatFraction(activity, goal);
        var weightChange = predictedWeight - startWeight;
        var fatMassNew = startFatMass + (weightChange * fatFraction);
        var minimumBodyFat = profile.Gender switch
        {
            Genders.Male => 4.0,
            Genders.Female => 12.0,
            _ => 8.0
        };

        var minimumFatMass = predictedWeight * minimumBodyFat / 100.0;
        fatMassNew = Math.Clamp(fatMassNew, minimumFatMass, predictedWeight);

        return Math.Clamp(fatMassNew / predictedWeight * 100.0, minimumBodyFat, 60.0);
    }

    private static (int GymCalories, int NonGymCalories) CalculateCalories(
        ProfileStep1ViewModel profile,
        ProfileStep2ViewModel activity,
        FitnessGoal goal)
    {
        var goalMonthlyDelta = GoalMonthlyDelta(goal);
        var fitnessMultiplier = FitnessMultiplier(activity.EveryDayFitnessLevel);
        var gymDays = Math.Clamp(CountBits((int)activity.ChosenGymDays), 0, 7);
        var gymBonus = gymDays * 0.1;
        var monthlyDeltaKg = (goalMonthlyDelta * fitnessMultiplier) +
                             (gymBonus * Math.Sign(goalMonthlyDelta));
        var caloriesPerDayFromGoal = monthlyDeltaKg * 7700.0 / 30.0;
        var tdee = EstimateTdee(profile, activity);
        var minimumCalories = profile.Gender == Genders.Female ? 1200 : 1500;
        var averageTarget = Math.Max(tdee + caloriesPerDayFromGoal, minimumCalories);
        var nonGymDays = 7 - gymDays;

        if (gymDays == 0 || nonGymDays == 0)
        {
            var calories = Math.Max((int)Math.Round(averageTarget), minimumCalories);
            return (calories, calories);
        }

        var shift = (int)Math.Round(Math.Clamp(tdee * 0.06, 120, 300));
        var weeklyTarget = averageTarget * 7.0;
        var gymCalories = (int)Math.Round(averageTarget + shift);
        var nonGymCalories = (int)Math.Round(
            (weeklyTarget - (gymCalories * gymDays)) / nonGymDays);

        if (nonGymCalories < minimumCalories)
        {
            nonGymCalories = minimumCalories;
            gymCalories = (int)Math.Round(
                (weeklyTarget - (nonGymCalories * nonGymDays)) / gymDays);
        }

        return (Math.Max(gymCalories, minimumCalories), nonGymCalories);
    }

    private static double EstimateTdee(
        ProfileStep1ViewModel profile,
        ProfileStep2ViewModel activity)
    {
        var bmr = profile.Gender switch
        {
            Genders.Male => 10 * profile.WeightInKg + 6.25 * profile.HeightInCM - 5 * profile.Age + 5,
            Genders.Female => 10 * profile.WeightInKg + 6.25 * profile.HeightInCM - 5 * profile.Age - 161,
            _ => 10 * profile.WeightInKg + 6.25 * profile.HeightInCM - 5 * profile.Age - 78
        };

        var activityFactor = activity.EveryDayFitnessLevel switch
        {
            FitnessLevel.Low => 1.35,
            FitnessLevel.Medium => 1.50,
            FitnessLevel.High => 1.65,
            _ => 1.50
        };

        var gymDays = Math.Clamp(CountBits((int)activity.ChosenGymDays), 0, 7);
        activityFactor = Math.Clamp(activityFactor + gymDays * 0.02, 1.25, 1.90);

        return bmr * activityFactor;
    }

    private static double GetFatFraction(ProfileStep2ViewModel activity, FitnessGoal goal)
    {
        var (fat, lean) = goal switch
        {
            FitnessGoal.HeavyCut => (0.85, 0.15),
            FitnessGoal.LightCut => (0.90, 0.10),
            FitnessGoal.LightBulk => (0.35, 0.65),
            FitnessGoal.HeavyBulk => (0.45, 0.55),
            _ => (0.50, 0.50)
        };

        var adjustment = Math.Clamp(CountBits((int)activity.ChosenGymDays) * 0.01, 0.0, 0.05);

        if (goal is FitnessGoal.HeavyCut or FitnessGoal.LightCut)
        {
            fat = Math.Clamp(fat + adjustment, 0.75, 0.97);
        }
        else if (goal is FitnessGoal.LightBulk or FitnessGoal.HeavyBulk)
        {
            lean = Math.Clamp(lean + adjustment, 0.40, 0.85);
            fat = 1.0 - lean;
        }

        return fat;
    }

    private static double GoalMonthlyDelta(FitnessGoal goal) => goal switch
    {
        FitnessGoal.HeavyCut => -1.5,
        FitnessGoal.LightCut => -0.75,
        FitnessGoal.Maintain => 0.0,
        FitnessGoal.LightBulk => 0.5,
        FitnessGoal.HeavyBulk => 1.0,
        _ => 0.0
    };

    private static double FitnessMultiplier(FitnessLevel level) => level switch
    {
        FitnessLevel.Low => 0.9,
        FitnessLevel.Medium => 1.0,
        FitnessLevel.High => 1.1,
        _ => 1.0
    };

    private static double ProteinPerKg(FitnessGoal goal) => goal switch
    {
        FitnessGoal.HeavyCut => 2.2,
        FitnessGoal.LightCut => 2.0,
        FitnessGoal.Maintain => 1.8,
        FitnessGoal.LightBulk => 1.7,
        FitnessGoal.HeavyBulk => 1.6,
        _ => 1.8
    };

    private static double FatPerKg(FitnessGoal goal) => goal switch
    {
        FitnessGoal.HeavyCut or FitnessGoal.LightCut => 0.7,
        FitnessGoal.Maintain => 0.8,
        FitnessGoal.LightBulk or FitnessGoal.HeavyBulk => 0.9,
        _ => 0.8
    };

    private static int FiberFromCalories(int calories) =>
        ClampInt((int)Math.Round(14.0 * (calories / 1000.0)), 25, 45);

    private static int CarbsFrom(int calories, int protein, int fat)
    {
        var remaining = calories - (protein * 4) - (fat * 9);
        return Math.Max(0, (int)Math.Round(remaining / 4.0));
    }

    private static int ClampInt(int value, int minimum, int maximum) =>
        Math.Max(minimum, Math.Min(maximum, value));

    private static int CountBits(int value)
    {
        var count = 0;

        while (value != 0)
        {
            value &= value - 1;
            count++;
        }

        return count;
    }
}
