using InTakeWise.Helper;
using InTakeWise.Models;
using InTakeWise.Validation;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace InTakeWise.Data
{
    public class ApplicationDbContext : IdentityDbContext
    {
        public ApplicationDbContext(
            DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<UsersInformation> UsersInformation => Set<UsersInformation>();
        public DbSet<FoodItem> FoodItems => Set<FoodItem>();
        public DbSet<PantryItem> PantryItems => Set<PantryItem>();
        public DbSet<MealLogEntry> MealLogs => Set<MealLogEntry>();
        public DbSet<WorkoutLogEntry> WorkoutLogs => Set<WorkoutLogEntry>();
        public DbSet<ShoppingList> ShoppingLists => Set<ShoppingList>();
        public DbSet<ShoppingListItem> ShoppingListItems => Set<ShoppingListItem>();
        public DbSet<ShoppingMealDay> ShoppingMealDays => Set<ShoppingMealDay>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<UsersInformation>(entity =>
            {
                entity.Property(x => x.ProfileUserName)
                    .HasMaxLength(ValidationLimits.MaximumProfileNameCharacters)
                    .IsRequired();

                entity.HasIndex(x => x.UserId)
                    .IsUnique();

                entity.HasOne(x => x.User)
                    .WithMany()
                    .HasForeignKey(x => x.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.ToTable("UsersInformation", table =>
                {
                    table.HasCheckConstraint(
                        "CK_UsersInformation_ProfileRanges",
                        $"[Age] BETWEEN {ValidationLimits.MinimumProfileAge} AND {ValidationLimits.MaximumProfileAge} " +
                        $"AND [HeightInCM] BETWEEN {ValidationLimits.MinimumHeightCentimetres} AND {ValidationLimits.MaximumHeightCentimetres} " +
                        $"AND [WeightInKg] BETWEEN {ValidationLimits.MinimumWeightKilograms} AND {ValidationLimits.MaximumWeightKilograms}");

                    table.HasCheckConstraint(
                        "CK_UsersInformation_Enums",
                        "[Gender] BETWEEN 0 AND 3 " +
                        "AND [EveryDayFitnessLevel] BETWEEN 0 AND 2 " +
                        "AND [ChosenGymDays] BETWEEN 0 AND 127 " +
                        "AND [ChosenFitnessGoal] BETWEEN 0 AND 4");

                    table.HasCheckConstraint(
                        "CK_UsersInformation_Targets_NonNegative",
                        "[CaloriesTargetGymDay] >= 0 " +
                        "AND [ProteinTargetGymDay] >= 0 " +
                        "AND [CarbsTargetGymDay] >= 0 " +
                        "AND [FatTargetGymDay] >= 0 " +
                        "AND [FiberTargetGymDay] >= 0 " +
                        "AND [CaloriesTargetNonGymDay] >= 0 " +
                        "AND [ProteinTargetNonGymDay] >= 0 " +
                        "AND [CarbsTargetNonGymDay] >= 0 " +
                        "AND [FatTargetNonGymDay] >= 0 " +
                        "AND [FiberTargetNonGymDay] >= 0");
                });
            });

            modelBuilder.Entity<FoodItem>(entity =>
            {
                entity.Property(x => x.Name)
                    .HasMaxLength(ValidationLimits.MaximumFoodNameCharacters)
                    .IsRequired();

                entity.Property(x => x.NormalizedName)
                    .HasMaxLength(ValidationLimits.MaximumFoodNameCharacters)
                    .IsRequired();

                entity.HasIndex(x => x.NormalizedName)
                    .IsUnique();

                entity.ToTable("FoodItems", table =>
                    table.HasCheckConstraint(
                        "CK_FoodItems_Nutrition_NonNegative",
                        "([CaloriesPer100g] IS NULL OR [CaloriesPer100g] >= 0) " +
                        "AND ([ProteinPer100g] IS NULL OR [ProteinPer100g] >= 0) " +
                        "AND ([CarbsPer100g] IS NULL OR [CarbsPer100g] >= 0) " +
                        "AND ([FatPer100g] IS NULL OR [FatPer100g] >= 0) " +
                        "AND ([FiberPer100g] IS NULL OR [FiberPer100g] >= 0)"));
            });

            modelBuilder.Entity<PantryItem>(entity =>
            {
                entity.Property(x => x.Quantity)
                    .HasPrecision(18, 2);

                entity.Property(x => x.Unit)
                    .HasMaxLength(ValidationLimits.MaximumPantryUnitCharacters)
                    .IsRequired();

                entity.HasIndex(x => new { x.UserId, x.FoodItemId })
                    .IsUnique();

                entity.HasOne(x => x.User)
                    .WithMany()
                    .HasForeignKey(x => x.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(x => x.FoodItem)
                    .WithMany()
                    .HasForeignKey(x => x.FoodItemId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.ToTable("PantryItems", table =>
                    table.HasCheckConstraint(
                        "CK_PantryItems_Quantity_NonNegative",
                        "[Quantity] >= 0"));
            });

            modelBuilder.Entity<MealLogEntry>(entity =>
            {
                entity.Property(x => x.RawInput)
                    .HasMaxLength(ValidationLimits.MaximumAiLogInputCharacters);

                entity.Property(x => x.LogDateLocal)
                    .HasColumnType("date");

                entity.HasIndex(x => new { x.UserId, x.Timestamp });

                entity.HasIndex(x => new
                {
                    x.UserId,
                    x.LogDateLocal,
                    x.TimeEat
                })
                    .IsUnique();

                entity.HasOne(x => x.User)
                    .WithMany()
                    .HasForeignKey(x => x.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.ToTable("MealLogs", table =>
                {
                    table.HasCheckConstraint(
                        "CK_MealLogs_TimeEat",
                        $"[TimeEat] BETWEEN 0 AND {(int)MealType.Snack}");

                    table.HasCheckConstraint(
                        "CK_MealLogs_Nutrition_NonNegative",
                        "([Calories] IS NULL OR [Calories] >= 0) " +
                        "AND ([Protein] IS NULL OR [Protein] >= 0) " +
                        "AND ([Carbs] IS NULL OR [Carbs] >= 0) " +
                        "AND ([Fat] IS NULL OR [Fat] >= 0) " +
                        "AND ([Fiber] IS NULL OR [Fiber] >= 0)");
                });
            });

            modelBuilder.Entity<WorkoutLogEntry>(entity =>
            {
                entity.Property(x => x.RawInput)
                    .HasMaxLength(ValidationLimits.MaximumAiLogInputCharacters);

                entity.Property(x => x.ActivityType)
                    .HasMaxLength(ValidationLimits.MaximumWorkoutActivityCharacters);

                entity.Property(x => x.Intensity)
                    .HasMaxLength(ValidationLimits.MaximumWorkoutIntensityCharacters);

                entity.HasIndex(x => new { x.UserId, x.Timestamp });

                entity.HasOne(x => x.User)
                    .WithMany()
                    .HasForeignKey(x => x.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.ToTable("WorkoutLogs", table =>
                    table.HasCheckConstraint(
                        "CK_WorkoutLogs_Values_NonNegative",
                        "([DurationMinutes] IS NULL OR [DurationMinutes] >= 0) " +
                        "AND ([CaloriesBurned] IS NULL OR [CaloriesBurned] >= 0)"));
            });

            modelBuilder.Entity<ShoppingList>(entity =>
            {
                entity.HasIndex(x => x.UserId)
                    .IsUnique();

                entity.Property(x => x.WeekStartLocalDate)
                    .HasColumnType("date");

                entity.HasOne(x => x.User)
                    .WithMany()
                    .HasForeignKey(x => x.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasMany(x => x.Items)
                    .WithOne(x => x.ShoppingList)
                    .HasForeignKey(x => x.ShoppingListId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasMany(x => x.Meals)
                    .WithOne(x => x.ShoppingList)
                    .HasForeignKey(x => x.ShoppingListId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<ShoppingListItem>(entity =>
            {
                entity.Property(x => x.Quantity)
                    .HasPrecision(18, 2);

                entity.Property(x => x.Name)
                    .HasMaxLength(ValidationLimits.MaximumShoppingItemNameCharacters)
                    .IsRequired();

                entity.Property(x => x.Unit)
                    .HasMaxLength(ValidationLimits.MaximumPantryUnitCharacters)
                    .IsRequired();

                entity.HasOne(x => x.FoodItem)
                    .WithMany()
                    .HasForeignKey(x => x.FoodItemId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.ToTable("ShoppingListItems", table =>
                    table.HasCheckConstraint(
                        "CK_ShoppingListItems_Quantity_Positive",
                        "[Quantity] > 0"));
            });

            modelBuilder.Entity<ShoppingMealDay>(entity =>
            {
                entity.Property(x => x.Day)
                    .HasMaxLength(ValidationLimits.MaximumShoppingDayCharacters)
                    .IsRequired();

                entity.Property(x => x.Title)
                    .HasMaxLength(ValidationLimits.MaximumShoppingTitleCharacters)
                    .IsRequired();

                entity.Property(x => x.MealDetailsJson)
                    .IsRequired();

                entity.Property(x => x.MealDateLocal)
                    .HasColumnType("date");

                entity.HasIndex(x => new
                {
                    x.ShoppingListId,
                    x.MealDateLocal
                })
                    .IsUnique();

                entity.ToTable("ShoppingMealDays", table =>
                    table.HasCheckConstraint(
                        "CK_ShoppingMealDays_Nutrition_NonNegative",
                        "[Calories] >= 0 " +
                        "AND [ProteinGrams] >= 0 " +
                        "AND [CarbsGrams] >= 0 " +
                        "AND [FatGrams] >= 0"));
            });
        }
    }
}
