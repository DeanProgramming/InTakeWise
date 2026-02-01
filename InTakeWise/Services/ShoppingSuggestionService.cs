using InTakeWise.Models;
using InTakeWise.Services;

namespace InTakeWise.Services
{
    public class ShoppingSuggestionService : IShoppingSuggestionService
    {
        public Task<List<FoodItem>> GetShoppingListAsync(string userID, List<FoodItem> currentInHouse)
        {
            List<FoodItem> foodItems = new List<FoodItem>();

            FoodItem dummyData = new FoodItem();
            dummyData.Quantity = 1;
            dummyData.Name = "CHICKEN";
            dummyData.Unit = "G";

            foodItems.Add(dummyData);

            return Task.FromResult(foodItems);
        }
        public Task<List<ShoppingMealsWeekSuggestion>> GetWeekSummaryAsync(string userID, List<FoodItem> currentInHouse, List<FoodItem> itemsWithShopping)
        {
            List<ShoppingMealsWeekSuggestion> mealsPotential = new List<ShoppingMealsWeekSuggestion>();

            ShoppingMealsWeekSuggestion shoppingMealsWeekSuggestion = new ShoppingMealsWeekSuggestion();

            shoppingMealsWeekSuggestion.Day = "Monday";
            shoppingMealsWeekSuggestion.Title = "B: Eggs on toast, D: Chicken and Rice, T: Tuna Salad";
            shoppingMealsWeekSuggestion.Calories = 500;
            shoppingMealsWeekSuggestion.ProteinGrams = 25;
            shoppingMealsWeekSuggestion.CarbsGrams = 150;
            shoppingMealsWeekSuggestion.FatGrams = 10;

            mealsPotential.Add(shoppingMealsWeekSuggestion);

            return Task.FromResult(mealsPotential);
        }
    } 
}

