namespace SmartLunch.Models
{
    // Table d'association : quels repas sont proposés quel jour
    public class DailyMenuMeal
    {
        public int DailyMenuId { get; set; }
        public DailyMenu? DailyMenu { get; set; }

        public int MealId { get; set; }
        public Meal? Meal { get; set; }
    }
}
