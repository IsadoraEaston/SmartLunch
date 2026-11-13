using System.ComponentModel.DataAnnotations;

namespace SmartLunch.Models
{
    // Message d'un parent à l'administrateur au sujet d'un enfant
    public class ContactMessage
    {
        public int Id { get; set; }

        // Le parent qui écrit
        [Required]
        public string ParentId { get; set; } = string.Empty;
        public ApplicationUser? Parent { get; set; }

        // L'enfant choisi dans la liste « Choix de l'enfant »
        [Display(Name = "Enfant")]
        public int ChildId { get; set; }
        public Child? Child { get; set; }

        [Required, StringLength(2000)]
        [Display(Name = "Message")]
        public string Message { get; set; } = string.Empty;

        public DateTime SentAt { get; set; } = DateTime.Now;

        // L'administrateur l'a-t-il traité ?
        [Display(Name = "Lu")]
        public bool IsRead { get; set; }
    }
}