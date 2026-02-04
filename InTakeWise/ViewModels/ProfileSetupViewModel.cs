namespace InTakeWise.ViewModels
{
    public class ProfileStep1ViewModel
    {
        public int Age { get; set; }
        public double HeightCm { get; set; }
        public string Gender { get; set; } = "";
        public double WeightKg { get; set; }
    }

    public class ProfileStep2ViewModel
    { 
        public string ActivityLevel { get; set; } = "Moderate"; 
        public int GymDaysPerWeek { get; set; }
        public string WorkoutDaysCsv { get; set; } = "";
    }

    public class ProfileStep3ViewModel
    {
        public double CurrentWeightKg { get; set; }
        public double PredictedWeightKg { get; set; }
        public int Months { get; set; }
    }

}
