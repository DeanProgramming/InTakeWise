namespace InTakeWise.Services
{
    public class MealProcessor
    {
        public class MealInfo
        {
            public int? Calories { get; set; }

            public int? Protein { get; set; }
            public int? Carbs { get; set; }
            public int? Fat { get; set; }
            public int? Fiber { get; set; }
        } 

        public static MealInfo MealProcessed(string userInput) {

            /* Meal processing will figure out later !  */

            return new MealInfo
            {
                Calories = 120,
                Protein = 120,
                Carbs = 120,
                Fat = 120,
                Fiber = 120
            };        
        }
    }
}