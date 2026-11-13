using Microsoft.AspNetCore.Identity;

namespace SmartLunch.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;

        // Solde de crédits du parent (remboursements d'annulations)
        public decimal CreditBalance { get; set; }

        // Un parent peut avoir plusieurs enfants et plusieurs transactions de crédit
        public ICollection<Child> Children { get; set; } = new List<Child>();
        public ICollection<CreditTransaction> CreditTransactions { get; set; } = new List<CreditTransaction>();
    }
}
