namespace SmartLunch.Models
{
    // Ex. : Lucas (ChildId 2) est allergique au gluten (AllergyId 3)
    public class ChildAllergy
    {
        public int ChildId { get; set; }
        public Child? Child { get; set; }

        public int AllergyId { get; set; }
        public Allergy? Allergy { get; set; }
    }
}