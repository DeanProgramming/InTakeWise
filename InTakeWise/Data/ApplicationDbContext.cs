using InTakeWise.Models;
using Microsoft.AspNetCore.Identity;
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

        protected override void OnModelCreating(ModelBuilder b)
        {
            base.OnModelCreating(b);

            // UsersInformation: one row per Identity user
            b.Entity<UsersInformation>()
                .HasIndex(x => x.UserId)
                .IsUnique();

            // PantryItem: prevent duplicates (same user + same food item)
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
        }
    }
}
