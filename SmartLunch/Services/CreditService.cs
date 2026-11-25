using Microsoft.EntityFrameworkCore;
using SmartLunch.Data;
using SmartLunch.Models;

namespace SmartLunch.Services
{
    public class CreditService : ICreditService
    {
        private readonly ApplicationDbContext _db;

        public CreditService(ApplicationDbContext db)
        {
            _db = db;
        }

        // Annule la commande ET ajoute le crédit en une seule sauvegarde (tout ou rien).
        // Le montant est relu dans la commande en base (contrainte 8).
        public async Task<OrderResult> RefundOrderAsync(int orderId, CreditTransactionType type, string reason)
        {
            if (type == CreditTransactionType.Usage)
            {
                return OrderResult.Fail("Type de remboursement invalide.");
            }

            var order = await _db.Orders
                .Include(o => o.Child)
                .FirstOrDefaultAsync(o => o.Id == orderId);
            if (order == null || order.Child == null)
            {
                return OrderResult.Fail("Commande introuvable.");
            }

            // Contrainte 6 : jamais de 2e crédit pour la même commande
            var alreadyRefunded = await _db.CreditTransactions.AnyAsync(c =>
                c.OrderId == orderId &&
                (c.Type == CreditTransactionType.Refund || c.Type == CreditTransactionType.ClosureRefund));
            if (alreadyRefunded || order.Status != OrderStatus.Active)
            {
                return OrderResult.Fail("Cette commande est déjà annulée et remboursée.");
            }

            var parent = await _db.Users.FirstOrDefaultAsync(u => u.Id == order.Child.ParentId);
            if (parent == null)
            {
                return OrderResult.Fail("Parent introuvable.");
            }

            var amount = order.TotalPrice;

            order.Status = OrderStatus.Cancelled;
            order.CancelledAt = DateTime.Now;

            _db.CreditTransactions.Add(NewTransaction(parent.Id, order.Id, amount, type, reason));
            parent.CreditBalance += amount;

            try
            {
                await _db.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                // L'index unique a bloqué un 2e crédit (deux clics en même temps, par exemple)
                return OrderResult.Fail("Cette commande est déjà remboursée.");
            }

            return OrderResult.Ok(orderId: order.Id, creditedAmount: amount);
        }

        // Section 3.3 : annule toutes les commandes actives du jour, rembourse, ferme la journée
        public async Task<int> CloseDayAsync(DateOnly date, string reason)
        {
            var menu = await _db.DailyMenus.FirstOrDefaultAsync(d => d.Date == date);
            if (menu == null)
            {
                return 0;
            }

            await using var tx = await _db.Database.BeginTransactionAsync();

            var orders = await _db.Orders
                .Include(o => o.Child)
                .Where(o => o.Date == date && o.Status == OrderStatus.Active)
                .ToListAsync();

            var parentIds = orders.Select(o => o.Child!.ParentId).Distinct().ToList();
            var parents = await _db.Users
                .Where(u => parentIds.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id);

            foreach (var order in orders)
            {
                var parent = parents[order.Child!.ParentId];

                order.Status = OrderStatus.Cancelled;
                order.CancelledAt = DateTime.Now;

                _db.CreditTransactions.Add(NewTransaction(
                    parent.Id, order.Id, order.TotalPrice, CreditTransactionType.ClosureRefund, reason));
                parent.CreditBalance += order.TotalPrice;
            }

            // La journée devient fermée : plus aucune commande possible (contrainte 7)
            menu.IsClosed = true;
            menu.ClosedReason = Truncate(reason, 200);
            await _db.DailyMenuMeals.Where(dm => dm.DailyMenuId == menu.Id).ExecuteDeleteAsync();

            await _db.SaveChangesAsync();
            await tx.CommitAsync();

            return orders.Count;
        }

        // Déduit du crédit pour payer une commande. Renvoie le montant réellement utilisé.
        public async Task<decimal> UseCreditAsync(string parentId, int orderId, decimal maxAmount)
        {
            if (maxAmount <= 0)
            {
                return 0m;
            }

            var order = await _db.Orders
                .Include(o => o.Child)
                .FirstOrDefaultAsync(o => o.Id == orderId);
            if (order == null || order.Child == null || order.Child.ParentId != parentId)
            {
                return 0m;
            }

            var parent = await _db.Users.FirstOrDefaultAsync(u => u.Id == parentId);
            if (parent == null || parent.CreditBalance <= 0)
            {
                return 0m;
            }

            var amount = Math.Min(parent.CreditBalance, maxAmount);

            _db.CreditTransactions.Add(NewTransaction(
                parentId, orderId, -amount, CreditTransactionType.Usage, "Crédit utilisé pour une commande"));
            parent.CreditBalance -= amount;

            await _db.SaveChangesAsync();
            return amount;
        }

        public async Task<decimal> GetBalanceAsync(string parentId)
        {
            return await _db.Users
                .Where(u => u.Id == parentId)
                .Select(u => u.CreditBalance)
                .FirstOrDefaultAsync();
        }

        public async Task<List<CreditTransaction>> GetTransactionsAsync(string parentId)
        {
            return await _db.CreditTransactions
                .Where(c => c.ParentId == parentId)
                .OrderByDescending(c => c.Date)
                .ThenByDescending(c => c.Id)
                .ToListAsync();
        }

        private static CreditTransaction NewTransaction(
            string parentId, int orderId, decimal amount, CreditTransactionType type, string reason)
        {
            return new CreditTransaction
            {
                ParentId = parentId,
                OrderId = orderId,
                Amount = amount,
                Type = type,
                Date = DateTime.Now,
                Reason = Truncate(reason, 200)
            };
        }

        private static string Truncate(string text, int max)
            => text.Length <= max ? text : text.Substring(0, max);
    }
}