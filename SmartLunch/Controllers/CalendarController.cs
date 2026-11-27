using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SmartLunch.Data;
using SmartLunch.Models;
using SmartLunch.Services;

namespace SmartLunch.Controllers
{
    [Authorize(Roles = IdentitySeeder.ParentRole)]
    public class CalendarController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IOrderService _orderService;

        public CalendarController(ApplicationDbContext context,
                                  UserManager<ApplicationUser> userManager,
                                  IOrderService orderService)
        {
            _context = context;
            _userManager = userManager;
            _orderService = orderService;
        }

        // GET: /Calendar?childId=1&year=2026&month=11
        public async Task<IActionResult> Index(int? childId, int? year, int? month)
        {
            var parentId = _userManager.GetUserId(User)!;

            // --- Les enfants de CE parent seulement ---
            var children = await _context.Children
                .Where(c => c.ParentId == parentId)
                .OrderBy(c => c.FirstName)
                .ToListAsync();

            if (!children.Any())
            {
                // Pas d'enfant : on l'envoie d'abord en inscrire un
                return RedirectToAction("Create", "Children");
            }

            // Aucun enfant demandé → le premier ; un enfant d'un autre parent → 404
            var child = childId == null
                ? children.First()
                : children.FirstOrDefault(c => c.Id == childId);
            if (child == null) return NotFound();

            // --- Le mois à afficher (le mois courant par défaut) ---
            var today = DateOnly.FromDateTime(DateTime.Now);
            var y = year ?? today.Year;
            var m = month ?? today.Month;
            if (m < 1 || m > 12 || y < 2000 || y > 2100) return BadRequest();

            var firstDay = new DateOnly(y, m, 1);
            var lastDay = firstDay.AddMonths(1).AddDays(-1);

            // --- Tout le mois en 5 requêtes (et non 5 requêtes par jour) ---

            // Les menus du mois, avec leurs repas et les allergènes de chaque repas
            var menus = await _context.DailyMenus
                .Where(d => d.Date >= firstDay && d.Date <= lastDay)
                .Include(d => d.DailyMenuMeals)
                    .ThenInclude(dm => dm.Meal)
                        .ThenInclude(meal => meal!.MealAllergies)
                            .ThenInclude(ma => ma.Allergy)
                .ToDictionaryAsync(d => d.Date);

            // Les commandes actives de l'enfant ce mois-ci (une seule par jour : contrainte 2)
            var orders = await _context.Orders
                .Where(o => o.ChildId == child.Id
                         && o.Status == OrderStatus.Active
                         && o.Date >= firstDay && o.Date <= lastDay)
                .ToDictionaryAsync(o => o.Date);

            // Les favoris de CET enfant
            var favoriteIds = (await _context.FavoriteMeals
                .Where(f => f.ChildId == child.Id)
                .Select(f => f.MealId)
                .ToListAsync()).ToHashSet();

            // Contrainte 4 : les allergies de l'enfant
            var childAllergyIds = (await _context.ChildAllergies
                .Where(ca => ca.ChildId == child.Id)
                .Select(ca => ca.AllergyId)
                .ToListAsync()).ToHashSet();

            // Les 3 repas les plus commandés (tous les enfants)
            var popularIds = (await _context.Orders
                .Where(o => o.Status == OrderStatus.Active)
                .GroupBy(o => o.MealId)
                .OrderByDescending(g => g.Count())
                .Select(g => g.Key)
                .Take(3)
                .ToListAsync()).ToHashSet();

            // --- La grille : une ligne par semaine, 5 cases (lundi → vendredi) ---
            var weeks = new List<List<CalendarDayViewModel?>>();
            var monday = firstDay.AddDays(-(((int)firstDay.DayOfWeek + 6) % 7));

            while (monday <= lastDay)
            {
                var week = new List<CalendarDayViewModel?>();
                for (var i = 0; i < 5; i++)
                {
                    var date = monday.AddDays(i);
                    week.Add(date.Month == m
                        ? BuildDay(date, menus, orders, favoriteIds, childAllergyIds, popularIds)
                        : null);   // jour d'un autre mois → case vide
                }
                if (week.Any(d => d != null)) weeks.Add(week);
                monday = monday.AddDays(7);
            }

            var vm = new CalendarViewModel
            {
                ChildId = child.Id,
                ChildName = child.FirstName,
                Children = children.Select(c => new SelectListItem
                {
                    Value = c.Id.ToString(),
                    Text = c.FirstName + " " + c.LastName,
                    Selected = c.Id == child.Id
                }).ToList(),
                Year = y,
                Month = m,
                ExtraPortionFee = _orderService.ExtraPortionFee,
                Weeks = weeks
            };

            return View(vm);
        }

        // Construit une case de jour (dans quel type tombe cette date ?)
        private CalendarDayViewModel BuildDay(
            DateOnly date,
            Dictionary<DateOnly, DailyMenu> menus,
            Dictionary<DateOnly, Order> orders,
            HashSet<int> favoriteIds,
            HashSet<int> childAllergyIds,
            HashSet<int> popularIds)
        {
            var day = new CalendarDayViewModel { Date = date };

            // La commande déjà passée, s'il y en a une
            if (orders.TryGetValue(date, out var order))
            {
                day.OrderedMealId = order.MealId;
                day.OrderedExtraPortion = order.HasExtraPortion;
                day.OrderedTotal = order.TotalPrice;
            }

            // Type 1 : pas de menu
            if (!menus.TryGetValue(date, out var menu))
            {
                day.Type = CalendarDayType.NoMenu;
                return day;
            }

            // Type 2 : journée fermée (vérifiée AVANT la date)
            if (menu.IsClosed)
            {
                day.Type = CalendarDayType.Closed;
                day.ClosedReason = menu.ClosedReason;
                return day;
            }

            // Types 3 et 4 : la même règle de 08 h 30 que l'OrderService
            day.Type = _orderService.CanStillChange(date)
                ? CalendarDayType.Open
                : CalendarDayType.Locked;

            day.Meals = menu.DailyMenuMeals
                .Where(dm => dm.Meal != null)
                .Select(dm => dm.Meal!)
                .OrderBy(meal => meal.Name)
                .Select(meal => new CalendarMealViewModel
                {
                    MealId = meal.Id,
                    Name = meal.Name,
                    Description = meal.Description,
                    Price = meal.Price,
                    IsVegetarian = meal.IsVegetarian,
                    IsFavorite = favoriteIds.Contains(meal.Id),
                    IsPopular = popularIds.Contains(meal.Id),
                    AllergyWarnings = meal.MealAllergies
                        .Where(ma => childAllergyIds.Contains(ma.AllergyId))
                        .Select(ma => ma.Allergy!.Name)
                        .ToList()
                })
                .ToList();

            return day;
        }
    }
}