namespace SmartLunch.Models
{
    // Ex. : les Pâtes Alfredo (MealId 10) contiennent des produits laitiers (AllergyId 2)
    public class MealAllergy
    {
        public int MealId { get; set; }
        public Meal? Meal { get; set; }

        public int AllergyId { get; set; }
        public Allergy? Allergy { get; set; }
    }
}
