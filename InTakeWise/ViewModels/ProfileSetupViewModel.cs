using System.ComponentModel.DataAnnotations;
using InTakeWise.Models;

namespace InTakeWise.ViewModels
{
    public class ProfileStep1ViewModel
    {
        [Required, MaxLength(25)]
        public string ProfileUserName { get; set; } = "";

        [Required, Range(1, 99)]
        public int Age { get; set; }

        [Required]
        public Genders Gender { get; set; }

        [Required, Range(1, 999)]
        public int HeightInCM { get; set; }

        [Required, Range(1, 999)]
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
        public FitnessGoal currentFitnessGoal = FitnessGoal.Maintain; 

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
