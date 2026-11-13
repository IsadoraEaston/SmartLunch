using System.ComponentModel.DataAnnotations;

namespace SmartLunch.Models
{
    public class DailyMenu
    {
        public int Id { get; set; }

        // Une seule ligne par date (rendue unique plus tard dans le DbContext)
        [DataType(DataType.Date)]
        [Display(Name = "Date")]
        public DateOnly Date { get; set; }

        // Journée pédagogique ou fermeture exceptionnelle (tempête, panne)
        [Display(Name = "Fermée")]
        public bool IsClosed { get; set; }

        [StringLength(200)]
        [Display(Name = "Raison de la fermeture")]
        public string? ClosedReason { get; set; }

        // Les 3 repas proposés ce jour-là
        public ICollection<DailyMenuMeal> DailyMenuMeals { get; set; } = new List<DailyMenuMeal>();
    }
}
