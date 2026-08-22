using System.ComponentModel.DataAnnotations;
using InTakeWise.Models;
using InTakeWise.Validation;

namespace InTakeWise.ViewModels
{
    public class ProfileStep1ViewModel
    {
        [Required(ErrorMessage = "Display name is required.")]
        [StringLength(
            ValidationLimits.MaximumProfileNameCharacters,
            MinimumLength = ValidationLimits.MinimumProfileNameCharacters)]
        [Display(Name = "Display name")]
        public string ProfileUserName { get; set; } = "";

        [Range(
            ValidationLimits.MinimumProfileAge,
            ValidationLimits.MaximumProfileAge,
            ErrorMessage = "InTakeWise is for adults aged 18 to 120.")]
        public int Age { get; set; }

        [Required(ErrorMessage = "Select a gender.")]
        public Genders? Gender { get; set; }

        [Range(
            ValidationLimits.MinimumHeightCentimetres,
            ValidationLimits.MaximumHeightCentimetres)]
        [Display(Name = "Height (cm)")]
        public int HeightInCM { get; set; }

        [Range(
            ValidationLimits.MinimumWeightKilograms,
            ValidationLimits.MaximumWeightKilograms)]
        [Display(Name = "Weight (kg)")]
        public int WeightInKg { get; set; }
    }

    public class ProfileStep2ViewModel
    {
        [Required(ErrorMessage = "Select an activity level.")]
        [Display(Name = "Activity level")]
        public FitnessLevel EveryDayFitnessLevel { get; set; } = FitnessLevel.Low;

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