using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SmartLunch.Models
{
    public class Meal
    {
        public int Id { get; set; }

        [Required, StringLength(100)]
        [Display(Name = "Nom")]
        public string Name { get; set; } = string.Empty;

        [StringLength(500)]
        [Display(Name = "Description")]
        public string? Description { get; set; }

        [Range(0, 100)]
        [Column(TypeName = "decimal(6,2)")]
        [DataType(DataType.Currency)]
        [Display(Name = "Prix")]
        public decimal Price { get; set; }

        [Display(Name = "Végétarien")]
        public bool IsVegetarian { get; set; }

        // Faux = retiré du catalogue, sans effacer les anciennes commandes
        [Display(Name = "Actif")]
        public bool IsActive { get; set; } = true;

        // Relations
        public ICollection<DailyMenuMeal> DailyMenuMeals { get; set; } = new List<DailyMenuMeal>();
        public ICollection<MealAllergy> MealAllergies { get; set; } = new List<MealAllergy>();
        public ICollection<FavoriteMeal> FavoriteMeals { get; set; } = new List<FavoriteMeal>();
        public ICollection<Order> Orders { get; set; } = new List<Order>();
    }
}