using SmartLunch.Models;

namespace SmartLunch.Services
{
    // Gère le solde de crédits d'un parent et l'historique (CreditTransaction).
    // Appelé par OrderService (annulation par le parent) et par l'admin (fermeture exceptionnelle).
    public interface ICreditService
    {
        // Ajoute un crédit au compte du parent après l'annulation d'une commande.
        // Refuse si la commande est déjà remboursée (contrainte 6 : pas de 2e crédit).
        // Le montant est relu dans la commande en base, jamais envoyé par le formulaire.
        Task<OrderResult> RefundOrderAsync(int orderId, CreditTransactionType type, string reason);

        // Annule toutes les commandes actives d'une journée et crédite chaque parent (section 3.3).
        // Marque aussi la journée comme fermée, ce qui bloque toute nouvelle commande.
        // Renvoie le nombre de commandes annulées.
        Task<int> CloseDayAsync(DateOnly date, string reason);

        // Utilise le crédit disponible pour payer une commande (type Usage, montant négatif).
        // Renvoie le montant réellement déduit (0 si le solde est nul).
        Task<decimal> UseCreditAsync(string parentId, int orderId, decimal maxAmount);

        // Solde actuel du parent.
        Task<decimal> GetBalanceAsync(string parentId);

        // Historique des transactions du parent, les plus récentes d'abord.
        Task<List<CreditTransaction>> GetTransactionsAsync(string parentId);
    }
}