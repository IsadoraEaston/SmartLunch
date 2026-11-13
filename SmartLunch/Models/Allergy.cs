using System.ComponentModel.DataAnnotations;

namespace SmartLunch.Models
{
    // Ex. : Arachides, Produits laitiers, Gluten
    public class Allergy
    {
        public int Id { get; set; }

        [Required, StringLength(50)]
        [Display(Name = "Allergie")]
        public string Name { get; set; } = string.Empty;

        // Les enfants qui ont cette allergie
        public ICollection<ChildAllergy> ChildAllergies { get; set; } = new List<ChildAllergy>();

        // Les repas qui contiennent cet allergène
        public ICollection<MealAllergy> MealAllergies { get; set; } = new List<MealAllergy>();
    }
}
