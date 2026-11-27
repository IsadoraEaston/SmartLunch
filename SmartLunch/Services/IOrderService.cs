using SmartLunch.Models;

namespace SmartLunch.Services
{
    // Point de rendez-vous entre l'écran de commande (Doris) et la logique métier (Xuefeng).
    // Toutes les règles sont appliquées côté serveur : les formulaires n'envoient que des Id.
    public interface IOrderService
    {
        // Crée une commande pour un enfant et une date.
        // Refuse si : l'enfant n'appartient pas au parent, la journée est fermée ou absente,
        // le repas n'est pas dans le menu du jour, ou l'enfant a déjà une commande active ce jour-là.
        // Le prix est relu en base et copié dans la commande (UnitPrice, ExtraFee, TotalPrice).
        Task<OrderResult> PlaceOrderAsync(string parentId, int childId, DateOnly date, int mealId, bool extraPortion);

        // Modifie le repas ou la portion supplémentaire d'une commande active.
        // Refuse le jour même après 08 h 30 et pour les jours passés.
        // Aucune limite pour les jours futurs.
        Task<OrderResult> ChangeOrderAsync(string parentId, int orderId, int newMealId, bool extraPortion);

        // Annule une commande active et ajoute un crédit au compte du parent.
        // Même règle de délai que la modification. Une commande déjà annulée ne génère pas de 2e crédit.
        Task<OrderResult> CancelOrderAsync(string parentId, int orderId);

        // Vrai si on peut encore commander, modifier ou annuler ce jour-là (règle de 08 h 30).
        // Utilisé par le calendrier pour afficher ou non les boutons radio.
        bool CanStillChange(DateOnly date);

        // Frais de la portion supplémentaire (lus dans appsettings.json), pour l'affichage.
        decimal ExtraPortionFee { get; }
        // Calcule le total : prix du repas + frais de la portion supplémentaire (lu dans appsettings.json).
        decimal ComputeTotal(Meal meal, bool extraPortion);
    }
}