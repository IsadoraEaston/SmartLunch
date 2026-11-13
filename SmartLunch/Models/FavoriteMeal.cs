namespace SmartLunch.Models
{
    // Un repas ajouté aux favoris d'un enfant (cœur dans le calendrier)
    public class FavoriteMeal
    {
        public int ChildId { get; set; }
        public Child? Child { get; set; }

        public int MealId { get; set; }
        public Meal? Meal { get; set; }
    }
}
