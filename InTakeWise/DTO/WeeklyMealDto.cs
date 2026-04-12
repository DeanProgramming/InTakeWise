using System.ComponentModel.DataAnnotations;

namespace InTakeWise.Dto
{
    public class WeeklyMealDto
    {
        public string Day { get; set; } = "";
        public bool IsGymDay { get; set; }
        public string Title { get; set; } = "";
        public int Calories { get; set; }
        public int ProteinGrams { get; set; }
        public int CarbsGrams { get; set; }
        public int FatGrams { get; set; }

        public DailyMealDetailsDto MealDetails { get; set; } = new();
    }

    public class DailyMealDetailsDto
    {
        public MealDetailDto Breakfast { get; set; } = new();
        public MealDetailDto Lunch { get; set; } = new();
        public MealDetailDto Dinner { get; set; } = new();
        public MealDetailDto Snack { get; set; } = new();
        public MealDetailDto LateSnack { get; set; } = new();
    }

    public class MealDetailDto
    {
        public string Overview { get; set; } = "";
        public List<string> Ingredients { get; set; } = new();
        public List<string> Steps { get; set; } = new();
    }

    public sealed class MealAnalysisDto
    {
        public string Summary { get; set; } = "";
        public int Calories { get; set; }
        public int Protein { get; set; }
        public int Carbs { get; set; }
        public int Fat { get; set; }
        public int Fiber { get; set; }
    }

    public sealed class WorkoutAnalysisDto
    {
        public string ActivityType { get; set; } = "";
        public int DurationMinutes { get; set; }
        public string Intensity { get; set; } = "";
        public int CaloriesBurned { get; set; }
    }

    public sealed class DailyLogSummaryDto
    {
        public bool IsGymDay { get; set; }

        public int CaloriesTarget { get; set; }
        public int ProteinTarget { get; set; }
        public int CarbsTarget { get; set; }
        public int FatTarget { get; set; }
        public int FiberTarget { get; set; }

        public int CaloriesEaten { get; set; }
        public int ProteinEaten { get; set; }
        public int CarbsEaten { get; set; }
        public int FatEaten { get; set; }
        public int FiberEaten { get; set; }

        public int CaloriesBurned { get; set; }

        public int NetCalories => CaloriesEaten - CaloriesBurned;

        public int RemainingCalories => CaloriesTarget - CaloriesEaten;
        public int RemainingProtein => ProteinTarget - ProteinEaten;
        public int RemainingCarbs => CarbsTarget - CarbsEaten;
        public int RemainingFat => FatTarget - FatEaten;
        public int RemainingFiber => FiberTarget - FiberEaten;
    }
}