using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SmartLunch.Models
{
    // Les deux états possibles d'une commande
    public enum OrderStatus
    {
        Active = 0,
        Cancelled = 1
    }

    public class Order
    {
        public int Id { get; set; }

        // Pour quel enfant
        public int ChildId { get; set; }
        public Child? Child { get; set; }

        // Quel repas
        public int MealId { get; set; }
        public Meal? Meal { get; set; }

        // Pour quel jour
        [DataType(DataType.Date)]
        [Display(Name = "Date du repas")]
        public DateOnly Date { get; set; }

        [Display(Name = "Portion supplémentaire")]
        public bool HasExtraPortion { get; set; }

        // Prix copiés au moment de la commande (calculés côté serveur)
        [Column(TypeName = "decimal(6,2)")]
        public decimal UnitPrice { get; set; }

        [Column(TypeName = "decimal(6,2)")]
        public decimal ExtraFee { get; set; }

        [Column(TypeName = "decimal(6,2)")]
        [Display(Name = "Total")]
        public decimal TotalPrice { get; set; }

        public OrderStatus Status { get; set; } = OrderStatus.Active;

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? CancelledAt { get; set; }
    }
}
