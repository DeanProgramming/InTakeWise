using System.ComponentModel.DataAnnotations;
using InTakeWise.Validation;
using Microsoft.AspNetCore.Identity;

namespace InTakeWise.Models
{
    public enum Genders { Male, Female, Other, PreferNotToSay }
    public enum FitnessLevel { Low, Medium, High }
    public enum FitnessGoal { HeavyCut, LightCut, Maintain, LightBulk, HeavyBulk }

    [Flags]
    public enum GymDays
    {
        None = 0,
        Monday = 1,
        Tuesday = 2,
        Wednesday = 4,
        Thursday = 8,
        Friday = 16,
        Saturday = 32,
        Sunday = 64
    }


    public class UsersInformation
    {
        public int Id { get; set; }

        [Required, MaxLength(450)]
        public string UserId { get; set; } = default!;

        public IdentityUser User { get; set; } = default!;

        [Required, StringLength(
            ValidationLimits.MaximumProfileNameCharacters,
            MinimumLength = ValidationLimits.MinimumProfileNameCharacters)]
        public string ProfileUserName { get; set; } = default!;

        [Range(
            ValidationLimits.MinimumProfileAge,
            ValidationLimits.MaximumProfileAge)]
        public int Age { get; set; }

        [Required]
        public Genders Gender { get; set; }

        [Range(
            ValidationLimits.MinimumWeightKilograms,
            ValidationLimits.MaximumWeightKilograms)]
        public int WeightInKg { get; set; }

        [Range(
            ValidationLimits.MinimumHeightCentimetres,
            ValidationLimits.MaximumHeightCentimetres)]
        public int HeightInCM { get; set; }

        [Required]
        public FitnessLevel EveryDayFitnessLevel { get; set; }
        [Required]
        public GymDays ChosenGymDays { get; set; } = GymDays.None;
        [Required]
        public FitnessGoal ChosenFitnessGoal { get; set; } = FitnessGoal.Maintain;


        // Gym day targets
        public int CaloriesTargetGymDay { get; set; }
        public int ProteinTargetGymDay { get; set; }
        public int CarbsTargetGymDay { get; set; }
        public int FatTargetGymDay { get; set; }
        public int FiberTargetGymDay { get; set; }

        // Non-gym day targets
        public int CaloriesTargetNonGymDay { get; set; }
        public int ProteinTargetNonGymDay { get; set; }
        public int CarbsTargetNonGymDay { get; set; }
        public int FatTargetNonGymDay { get; set; }
        public int FiberTargetNonGymDay { get; set; }
    }
}
