using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace SmartLunch.Models
{
    // Le formulaire Contact : seulement ce que le parent choisit (ta Q1)
    public class ContactFormViewModel
    {
        [Required(ErrorMessage = "Choisissez un enfant.")]
        [Display(Name = "Enfant")]
        public int? ChildId { get; set; }

        // Pas de message vide, pas de texte énorme
        [Required(ErrorMessage = "Le message ne peut pas être vide.")]
        [StringLength(2000, ErrorMessage = "Le message ne peut pas dépasser 2000 caractères.")]
        [Display(Name = "Message")]
        public string Message { get; set; } = string.Empty;

        // Pour la liste « Choix de l'enfant » (Un parent: Seulement SES enfants)
        public List<SelectListItem> Children { get; set; } = new();

        //L'historique des messages envoyés
        public List<ContactMessage> SentMessages { get; set; } = new();
    }
}