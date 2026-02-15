namespace InTakeWise.ViewModels
{
    public class MenuSideViewModel
    {
        public string? UserName { get; set; }

        public bool IsGymDayToday { get; set; }

        public int CaloriesTarget { get; set; }
        public int ProteinTarget { get; set; }
        public int CarbsTarget { get; set; }
        public int FatTarget { get; set; }
        public int FiberTarget { get; set; }

        public int CaloriesConsumed { get; set; }
        public int ProteinConsumed { get; set; }
        public int CarbsConsumed { get; set; }
        public int FatConsumed { get; set; }
        public int FiberConsumed { get; set; }
    }
}
