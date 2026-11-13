using System.ComponentModel.DataAnnotations;

namespace SmartLunch.Models
{
    public class Child
    {
        public int Id { get; set; }

        [Required, StringLength(50)]
        [Display(Name = "Prénom")]
        public string FirstName { get; set; } = string.Empty;

        [Required, StringLength(50)]
        [Display(Name = "Nom")]
        public string LastName { get; set; } = string.Empty;

        // Ex. : E-31, E-42
        [Required, StringLength(10)]
        [Display(Name = "Classe")]
        public string ClassNumber { get; set; } = string.Empty;

        // Ex. : sans ketchup, sans fromage
        [StringLength(500)]
        [Display(Name = "Préférences alimentaires")]
        public string? FoodPreferences { get; set; }

        // Le parent de l'enfant (clé étrangère vers ApplicationUser)
        [Required]
        public string ParentId { get; set; } = string.Empty;
        public ApplicationUser? Parent { get; set; }

        // Un enfant a plusieurs commandes, favoris et allergies
        public ICollection<Order> Orders { get; set; } = new List<Order>();
        public ICollection<FavoriteMeal> FavoriteMeals { get; set; } = new List<FavoriteMeal>();
        public ICollection<ChildAllergy> ChildAllergies { get; set; } = new List<ChildAllergy>();
    }
}