using Microsoft.EntityFrameworkCore;
using SmartLunch.Data;
using SmartLunch.Models;

namespace SmartLunch.Services
{
    public class OrderService : IOrderService
    {
        private readonly ApplicationDbContext _db;
        private readonly ICreditService _credit;
        private readonly IConfiguration _config;

        public OrderService(ApplicationDbContext db, ICreditService credit, IConfiguration config)
        {
            _db = db;
            _credit = credit;
            _config = config;
        }

        // Valeurs lues côté serveur (appsettings.json), jamais envoyées par le formulaire
        private decimal ExtraFee => _config.GetValue<decimal>("SmartLunch:ExtraPortionFee");
        private TimeOnly Cutoff => new TimeOnly(
            _config.GetValue("SmartLunch:OrderCutoffHour", 8),
            _config.GetValue("SmartLunch:OrderCutoffMinute", 30));

        public decimal ComputeTotal(Meal meal, bool extraPortion)
            => meal.Price + (extraPortion ? ExtraFee : 0m);

        // Même règle que CutoffError : une seule source de vérité pour le calendrier et le service
        public bool CanStillChange(DateOnly date) => CutoffError(date, "modifier") == null;

        public decimal ExtraPortionFee => ExtraFee;

        public async Task<OrderResult> PlaceOrderAsync(
            string parentId, int childId, DateOnly date, int mealId, bool extraPortion)
        {
            // Contrainte 1 : l'enfant doit appartenir à ce parent
            var child = await _db.Children.FirstOrDefaultAsync(c => c.Id == childId && c.ParentId == parentId);
            if (child == null) return OrderResult.Fail("Enfant introuvable.");

            var timeError = CutoffError(date, "commander");
            if (timeError != null) return OrderResult.Fail(timeError);

            // Contrainte 7 : journée fermée ; le repas doit être actif et au menu
            var (meal, mealError) = await CheckMealAsync(date, mealId);
            if (meal == null) return OrderResult.Fail(mealError!);

            // Contrainte 2 : une seule commande active par enfant et par jour
            var alreadyOrdered = await _db.Orders.AnyAsync(o =>
                o.ChildId == childId && o.Date == date && o.Status == OrderStatus.Active);
            if (alreadyOrdered)
                return OrderResult.Fail("Cet enfant a déjà une commande pour cette journée. Modifiez-la ou annulez-la.");

            var (order, createError) = await CreateOrderAsync(child, date, meal, extraPortion);
            if (order == null) return OrderResult.Fail(createError!);

            var creditUsed = await _credit.UseCreditAsync(parentId, order.Id, order.TotalPrice);
            return OrderResult.Ok(orderId: order.Id, totalPrice: order.TotalPrice, creditUsed: creditUsed);
        }

        // Modifier = annuler l'ancienne commande (crédit) puis créer la nouvelle, en une transaction
        public async Task<OrderResult> ChangeOrderAsync(
            string parentId, int orderId, int newMealId, bool extraPortion)
        {
            var order = await _db.Orders
                .Include(o => o.Child)
                .FirstOrDefaultAsync(o => o.Id == orderId && o.Child!.ParentId == parentId);
            if (order == null || order.Child == null) return OrderResult.Fail("Commande introuvable.");
            if (order.Status != OrderStatus.Active) return OrderResult.Fail("Cette commande est déjà annulée.");

            var timeError = CutoffError(order.Date, "modifier");
            if (timeError != null) return OrderResult.Fail(timeError);

            var (meal, mealError) = await CheckMealAsync(order.Date, newMealId);
            if (meal == null) return OrderResult.Fail(mealError!);

            var child = order.Child;
            var date = order.Date;

            await using var tx = await _db.Database.BeginTransactionAsync();

            var refund = await _credit.RefundOrderAsync(order.Id, CreditTransactionType.Refund, "Modification de la commande");
            if (!refund.Success) return refund;

            var (newOrder, createError) = await CreateOrderAsync(child, date, meal, extraPortion);
            if (newOrder == null) return OrderResult.Fail(createError!);

            var creditUsed = await _credit.UseCreditAsync(parentId, newOrder.Id, newOrder.TotalPrice);
            await tx.CommitAsync();

            return OrderResult.Ok(orderId: newOrder.Id, totalPrice: newOrder.TotalPrice, creditUsed: creditUsed);
        }

        public async Task<OrderResult> CancelOrderAsync(string parentId, int orderId)
        {
            var order = await _db.Orders
                .Include(o => o.Child)
                .FirstOrDefaultAsync(o => o.Id == orderId && o.Child!.ParentId == parentId);
            if (order == null) return OrderResult.Fail("Commande introuvable.");
            if (order.Status != OrderStatus.Active) return OrderResult.Fail("Cette commande est déjà annulée.");

            var timeError = CutoffError(order.Date, "annuler");
            if (timeError != null) return OrderResult.Fail(timeError);

            return await _credit.RefundOrderAsync(order.Id, CreditTransactionType.Refund, "Annulation par le parent");
        }

        // Règle de l'enseignante : limite seulement le jour même (et les jours passés)
        private string? CutoffError(DateOnly date, string action)
        {
            var now = DateTime.Now;
            var today = DateOnly.FromDateTime(now);

            if (date < today)
                return "Cette journée est passée.";

            if (date == today && TimeOnly.FromDateTime(now) >= Cutoff)
                return $"Il est trop tard pour {action} : la limite est {Cutoff:HH:mm} le jour même.";

            return null;
        }

        private async Task<(Meal? Meal, string? Error)> CheckMealAsync(DateOnly date, int mealId)
        {
            var menu = await _db.DailyMenus
                .Include(d => d.DailyMenuMeals)
                .FirstOrDefaultAsync(d => d.Date == date);

            if (menu == null) return (null, "Aucun menu n'est prévu ce jour-là.");
            if (menu.IsClosed) return (null, "Cette journée est fermée : aucune commande possible.");
            if (!menu.DailyMenuMeals.Any(dm => dm.MealId == mealId))
                return (null, "Ce repas n'est pas au menu de cette journée.");

            var meal = await _db.Meals.FirstOrDefaultAsync(m => m.Id == mealId);
            if (meal == null || !meal.IsActive) return (null, "Ce repas n'est plus disponible.");

            return (meal, null);
        }

        // Contrainte 8 : les prix sont relus en base et copiés dans la commande
        private async Task<(Order? Order, string? Error)> CreateOrderAsync(
            Child child, DateOnly date, Meal meal, bool extraPortion)
        {
            var extra = extraPortion ? ExtraFee : 0m;
            var order = new Order
            {
                ChildId = child.Id,
                MealId = meal.Id,
                Date = date,
                HasExtraPortion = extraPortion,
                UnitPrice = meal.Price,
                ExtraFee = extra,
                TotalPrice = meal.Price + extra,
                Status = OrderStatus.Active,
                CreatedAt = DateTime.Now
            };

            _db.Orders.Add(order);
            try
            {
                await _db.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                // L'index unique a bloqué une 2e commande active (double clic, par exemple)
                _db.Entry(order).State = EntityState.Detached;
                return (null, "Cet enfant a déjà une commande pour cette journée.");
            }

            return (order, null);
        }
    }
}