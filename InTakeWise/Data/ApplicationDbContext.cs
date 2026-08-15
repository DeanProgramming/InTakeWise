using InTakeWise.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace InTakeWise.Data
{
    public class ApplicationDbContext : IdentityDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
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

        protected override void OnModelCreating(ModelBuilder b)
        {
            base.OnModelCreating(b);

            b.Entity<UsersInformation>()
                .HasIndex(x => x.UserId)
                .IsUnique();

            b.Entity<FoodItem>(e =>
            {
                e.Property(x => x.Name).HasMaxLength(100).IsRequired();
                e.Property(x => x.NormalizedName).HasMaxLength(100).IsRequired();
                e.HasIndex(x => x.NormalizedName).IsUnique();
            });

            b.Entity<PantryItem>()
                .HasIndex(x => new { x.UserId, x.FoodItemId })
                .IsUnique();

            b.Entity<PantryItem>()
                .Property(x => x.Quantity)
                .HasPrecision(18, 2);

            b.Entity<PantryItem>()
                .HasOne(x => x.FoodItem)
                .WithMany()
                .HasForeignKey(x => x.FoodItemId)
                .OnDelete(DeleteBehavior.Restrict);

            b.Entity<MealLogEntry>(e =>
            {
                e.ToTable("MealLogs");
                e.HasIndex(x => new { x.UserId, x.Timestamp });
            });

            b.Entity<WorkoutLogEntry>(e =>
            {
                e.ToTable("WorkoutLogs");
                e.HasIndex(x => new { x.UserId, x.Timestamp });
            });

            b.Entity<ShoppingList>(e =>
            {
                e.HasIndex(x => x.UserId).IsUnique();

                e.Property(x => x.WeekStartLocalDate).HasColumnType("date");

                e.HasMany(x => x.Items)
                    .WithOne(x => x.ShoppingList)
                    .HasForeignKey(x => x.ShoppingListId)
                    .OnDelete(DeleteBehavior.Cascade);

                e.HasMany(x => x.Meals)
                    .WithOne(x => x.ShoppingList)
                    .HasForeignKey(x => x.ShoppingListId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            b.Entity<ShoppingListItem>(e =>
            {
                e.Property(x => x.Quantity).HasPrecision(18, 2);
                e.Property(x => x.Name).HasMaxLength(200).IsRequired();
                e.Property(x => x.Unit).HasMaxLength(50);

                e.HasOne(x => x.FoodItem)
                    .WithMany()
                    .HasForeignKey(x => x.FoodItemId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            b.Entity<ShoppingMealDay>(e =>
            {
                e.Property(x => x.Day).HasMaxLength(20).IsRequired();
                e.Property(x => x.Title).HasMaxLength(4000).IsRequired();
                e.Property(x => x.MealDetailsJson).IsRequired();
                e.Property(x => x.MealDateLocal).HasColumnType("date");
            });
        }
    }
}