using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SmartLunch.Models
{
    // D'où vient (ou où va) le crédit
    public enum CreditTransactionType
    {
        Refund = 0,         // annulation par le parent avant 08 h 30
        ClosureRefund = 1,  // fermeture exceptionnelle de l'école
        Usage = 2           // crédit utilisé pour payer une commande
    }

    public class CreditTransaction
    {
        public int Id { get; set; }

        // Le parent concerné
        [Required]
        public string ParentId { get; set; } = string.Empty;
        public ApplicationUser? Parent { get; set; }

        // La commande liée (facultatif)
        public int? OrderId { get; set; }
        public Order? Order { get; set; }

        // Positif = crédit ajouté ; négatif = crédit utilisé
        [Column(TypeName = "decimal(6,2)")]
        [Display(Name = "Montant")]
        public decimal Amount { get; set; }

        public CreditTransactionType Type { get; set; }

        [Display(Name = "Date")]
        public DateTime Date { get; set; } = DateTime.Now;

        [StringLength(200)]
        [Display(Name = "Motif")]
        public string? Reason { get; set; }
    }
}