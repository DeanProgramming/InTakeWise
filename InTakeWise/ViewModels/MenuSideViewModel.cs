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

        public int CaloriesConsumed { get; set; } = 1500;
        public int ProteinConsumed { get; set; } = 120;
        public int CarbsConsumed { get; set; } = 200;
        public int FatConsumed { get; set; } = 24;
        public int FiberConsumed { get; set; } = 13;
    }
}
