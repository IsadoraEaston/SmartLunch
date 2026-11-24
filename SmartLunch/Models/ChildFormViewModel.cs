using System.ComponentModel.DataAnnotations;

namespace SmartLunch.Models
{
    // Ce que le formulaire « Ajouter / Modifier un enfant » envoie et reçoit
    public class ChildFormViewModel
    {
        public int? Id { get; set; }

        [Required(ErrorMessage = "Le prénom est obligatoire.")]
        [StringLength(50)]
        [Display(Name = "Prénom")]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Le nom est obligatoire.")]
        [StringLength(50)]
        [Display(Name = "Nom")]
        public string LastName { get; set; } = string.Empty;

        [Required(ErrorMessage = "La classe est obligatoire.")]
        [RegularExpression(@"^[A-Za-z]-\d{2}$", ErrorMessage = "Format attendu : une lettre, un tiret et deux chiffres (ex. E-31).")]
        [Display(Name = "Classe")]
        public string ClassNumber { get; set; } = string.Empty;

        [StringLength(500)]
        [Display(Name = "Préférences alimentaires")]
        public string? FoodPreferences { get; set; }

        // Les Id des allergies cochées par le parent
        public List<int> SelectedAllergyIds { get; set; } = new();

        // Toutes les allergies de la base, pour afficher les cases à cocher
        public List<Allergy> AvailableAllergies { get; set; } = new();
    }
}