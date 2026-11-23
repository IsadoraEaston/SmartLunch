namespace SmartLunch.Services
{
    // Résultat d'une opération sur une commande ou un crédit.
    // Le service ne lance pas d'exception pour une règle métier refusée :
    // il renvoie Success = false avec un message en français à afficher.
    public class OrderResult
    {
        public bool Success { get; private set; }

        // Message d'erreur affichable à l'utilisateur (null si Success)
        public string? Error { get; private set; }

        // Commande créée ou modifiée (null en cas d'échec ou d'annulation)
        public int? OrderId { get; private set; }

        // Montant total calculé côté serveur (contrainte 8)
        public decimal? TotalPrice { get; private set; }

        // Crédit ajouté au compte du parent (annulation), sinon null
        public decimal? CreditedAmount { get; private set; }

        public static OrderResult Ok(int? orderId = null, decimal? totalPrice = null, decimal? creditedAmount = null)
            => new OrderResult
            {
                Success = true,
                OrderId = orderId,
                TotalPrice = totalPrice,
                CreditedAmount = creditedAmount
            };

        public static OrderResult Fail(string error)
            => new OrderResult { Success = false, Error = error };
    }
}