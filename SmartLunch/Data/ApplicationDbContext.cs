using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SmartLunch.Models;

namespace SmartLunch.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // Une ligne DbSet = une table dans la base
        public DbSet<Child> Children { get; set; }
        public DbSet<Meal> Meals { get; set; }
        public DbSet<DailyMenu> DailyMenus { get; set; }
        public DbSet<DailyMenuMeal> DailyMenuMeals { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<FavoriteMeal> FavoriteMeals { get; set; }
        public DbSet<Allergy> Allergies { get; set; }
        public DbSet<ChildAllergy> ChildAllergies { get; set; }
        public DbSet<MealAllergy> MealAllergies { get; set; }
        public DbSet<CreditTransaction> CreditTransactions { get; set; }
        public DbSet<ContactMessage> ContactMessages { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            // Obligatoire : crée d'abord les tables d'Identity
            base.OnModelCreating(builder);

            // --- 1. Clés composées des tables d'association ---
            builder.Entity<DailyMenuMeal>().HasKey(x => new { x.DailyMenuId, x.MealId });
            builder.Entity<FavoriteMeal>().HasKey(x => new { x.ChildId, x.MealId });
            builder.Entity<ChildAllergy>().HasKey(x => new { x.ChildId, x.AllergyId });
            builder.Entity<MealAllergy>().HasKey(x => new { x.MealId, x.AllergyId });

            // --- 2. Index uniques ---
            // Une seule journée de menu par date
            builder.Entity<DailyMenu>().HasIndex(d => d.Date).IsUnique();

            // Pas deux allergies avec le même nom
            builder.Entity<Allergy>().HasIndex(a => a.Name).IsUnique();

            // Contrainte 2 : une seule commande ACTIVE par enfant et par jour
            builder.Entity<Order>()
                .HasIndex(o => new { o.ChildId, o.Date })
                .IsUnique()
                .HasFilter("[Status] = 0");

            // Contrainte 6 : un seul remboursement par commande
            builder.Entity<CreditTransaction>()
                .HasIndex(c => c.OrderId)
                .IsUnique()
                .HasFilter("[OrderId] IS NOT NULL AND [Type] IN (0, 1)");

            // --- 3. Comportement à la suppression ---
            // Interdit de supprimer un repas déjà commandé (on le désactive à la place)
            builder.Entity<Order>()
                .HasOne(o => o.Meal)
                .WithMany(m => m.Orders)
                .HasForeignKey(o => o.MealId)
                .OnDelete(DeleteBehavior.Restrict);

            // Évite un double chemin de suppression (parent → message et parent → enfant → message)
            builder.Entity<ContactMessage>()
                .HasOne(c => c.Child)
                .WithMany()
                .HasForeignKey(c => c.ChildId)
                .OnDelete(DeleteBehavior.Restrict);

            // --- 4. Précision de l'argent ---
            builder.Entity<ApplicationUser>()
                .Property(u => u.CreditBalance)
                .HasPrecision(8, 2);
        }
    }
}
