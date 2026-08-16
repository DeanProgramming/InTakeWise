using System.ComponentModel.DataAnnotations;
using InTakeWise.Models;
using InTakeWise.Validation;

namespace InTakeWise.ViewModels
{
    public class ProfileStep1ViewModel
    {
        [Required, StringLength(
            ValidationLimits.MaximumProfileNameCharacters,
            MinimumLength = ValidationLimits.MinimumProfileNameCharacters)]
        public string ProfileUserName { get; set; } = "";

        [Range(
            ValidationLimits.MinimumProfileAge,
            ValidationLimits.MaximumProfileAge)]
        public int Age { get; set; }

        [Required]
        public Genders Gender { get; set; }

        [Range(
            ValidationLimits.MinimumHeightCentimetres,
            ValidationLimits.MaximumHeightCentimetres)]
        public int HeightInCM { get; set; }

        [Range(
            ValidationLimits.MinimumWeightKilograms,
            ValidationLimits.MaximumWeightKilograms)]
        public int WeightInKg { get; set; }
    }

    public class ProfileStep2ViewModel
    {
        [Required]
        public FitnessLevel EveryDayFitnessLevel { get; set; }

        [Required]
        public GymDays ChosenGymDays { get; set; } = GymDays.None;
    }

    public class ProfileStep3ViewModel
    {
        public double CurrentWeightKg { get; set; }
        public double CurrentFatPercentage { get; set; }
        public Dictionary<FitnessGoal, double> PredictedWeightKgByGoal { get; set; } = new();
        public Dictionary<FitnessGoal, double> FatPercentageByGoal { get; set; } = new();
        public FitnessGoal CurrentFitnessGoal { get; set; } = FitnessGoal.Maintain;


        public int Months { get; set; }

        public string GoalLabel(FitnessGoal goal) => goal switch
        {
            FitnessGoal.HeavyCut => "Heavy Cut",
            FitnessGoal.LightCut => "Light Cut",
            FitnessGoal.Maintain => "Maintain",
            FitnessGoal.LightBulk => "Light Bulk",
            FitnessGoal.HeavyBulk => "Heavy Bulk",
            _ => goal.ToString()
        };

    }
}
