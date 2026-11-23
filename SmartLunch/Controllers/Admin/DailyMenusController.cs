using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartLunch.Data;
using SmartLunch.Models;

namespace SmartLunch.Controllers.Admin
{
    [Authorize(Roles = IdentitySeeder.AdministratorRole)]
    [Route("Admin/DailyMenus")]
    public class DailyMenusController : Controller
    {
        private readonly ApplicationDbContext _db;

        public DailyMenusController(ApplicationDbContext db)
        {
            _db = db;
        }

        // GET: Admin/DailyMenus
        [HttpGet("")]
        public async Task<IActionResult> Index()
        {
            var menus = await _db.DailyMenus
                .Include(d => d.DailyMenuMeals).ThenInclude(dm => dm.Meal)
                .OrderBy(d => d.Date)
                .ToListAsync();
            return View("~/Views/Admin/DailyMenus/Index.cshtml", menus);
        }

        // GET: Admin/DailyMenus/Edit/5
        [HttpGet("Edit/{id:int}")]
        public async Task<IActionResult> Edit(int id)
        {
            var menu = await _db.DailyMenus
                .Include(d => d.DailyMenuMeals)
                .FirstOrDefaultAsync(d => d.Id == id);
            if (menu == null) return NotFound();

            await LoadMealsAsync(menu.DailyMenuMeals.Select(dm => dm.MealId));
            return View("~/Views/Admin/DailyMenus/Edit.cshtml", menu);
        }

        // POST: Admin/DailyMenus/Edit/5
        [HttpPost("Edit/{id:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, bool isClosed, string? closedReason, int[] mealIds)
        {
            // Sans Include : on ne suit pas les anciens DailyMenuMeal
            var menu = await _db.DailyMenus.FirstOrDefaultAsync(d => d.Id == id);
            if (menu == null) return NotFound();

            var chosen = mealIds.Distinct().ToList();

            // Repas actuellement au menu de cette journée
            var currentMealIds = await _db.DailyMenuMeals
                .Where(dm => dm.DailyMenuId == id)
                .Select(dm => dm.MealId)
                .ToListAsync();

            // Commandes actives de cette journée, regroupées par repas
            var activeOrdersByMeal = await _db.Orders
                .Where(o => o.Date == menu.Date && o.Status == OrderStatus.Active)
                .GroupBy(o => o.MealId)
                .Select(g => new { MealId = g.Key, Count = g.Count() })
                .ToListAsync();

            if (isClosed)
            {
                // Une journée fermée n'a aucun repas
                if (string.IsNullOrWhiteSpace(closedReason))
                {
                    ModelState.AddModelError("", "Indiquez la raison de la fermeture.");
                }
                else if (closedReason.Length > 200)
                {
                    ModelState.AddModelError("", "La raison ne peut pas dépasser 200 caractères.");
                }
                else if (activeOrdersByMeal.Any())
                {
                    ModelState.AddModelError("",
                        "Cette journée a des commandes actives. Utilisez l'annulation exceptionnelle (qui rembourse les parents).");
                }
            }
            else
            {
                // Contrainte 3 : exactement 3 repas actifs et différents (validé côté serveur)
                if (chosen.Count != 3)
                {
                    ModelState.AddModelError("", "Une journée ouverte doit avoir exactement 3 repas différents.");
                }
                else
                {
                    var validCount = await _db.Meals.CountAsync(m => chosen.Contains(m.Id) && m.IsActive);
                    if (validCount != 3)
                    {
                        ModelState.AddModelError("", "Un des repas choisis n'existe pas ou est désactivé.");
                    }
                }

                // On ne retire pas un repas qui a des commandes actives ce jour-là
                foreach (var removedId in currentMealIds.Except(chosen))
                {
                    var orders = activeOrdersByMeal.FirstOrDefault(a => a.MealId == removedId);
                    if (orders != null)
                    {
                        var mealName = await _db.Meals
                            .Where(m => m.Id == removedId)
                            .Select(m => m.Name)
                            .FirstOrDefaultAsync();
                        ModelState.AddModelError("",
                            $"Impossible de retirer « {mealName} » : {orders.Count} commande(s) active(s) pour ce repas ce jour-là.");
                    }
                }
            }

            if (!ModelState.IsValid)
            {
                await LoadMealsAsync(chosen);
                menu.IsClosed = isClosed;
                menu.ClosedReason = closedReason;
                return View("~/Views/Admin/DailyMenus/Edit.cshtml", menu);
            }

            menu.IsClosed = isClosed;
            menu.ClosedReason = isClosed ? closedReason!.Trim() : null;

            // Remplace les repas : suppression directe en base, puis ajout des nouveaux
            await _db.DailyMenuMeals.Where(dm => dm.DailyMenuId == id).ExecuteDeleteAsync();
            if (!isClosed)
            {
                foreach (var mid in chosen)
                {
                    _db.DailyMenuMeals.Add(new DailyMenuMeal { DailyMenuId = id, MealId = mid });
                }
            }

            await _db.SaveChangesAsync();

            TempData["Success"] = $"Le menu du {menu.Date:yyyy-MM-dd} a été enregistré.";
            return RedirectToAction(nameof(Index));
        }

        // GET: Admin/DailyMenus/Create
        [HttpGet("Create")]
        public IActionResult Create()
        {
            return View("~/Views/Admin/DailyMenus/Create.cshtml");
        }

        // POST: Admin/DailyMenus/Create
        // Crée une journée fermée tant que 3 repas ne sont pas choisis
        [HttpPost("Create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(DateOnly date)
        {
            if (date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday)
            {
                ModelState.AddModelError("", "Les menus ne sont pas planifiés la fin de semaine.");
            }
            else if (await _db.DailyMenus.AnyAsync(d => d.Date == date))
            {
                ModelState.AddModelError("", "Cette date existe déjà.");
            }

            if (!ModelState.IsValid)
            {
                return View("~/Views/Admin/DailyMenus/Create.cshtml");
            }

            var menu = new DailyMenu
            {
                Date = date,
                IsClosed = true,
                ClosedReason = "Menu à planifier"
            };
            _db.DailyMenus.Add(menu);
            await _db.SaveChangesAsync();

            return RedirectToAction(nameof(Edit), new { id = menu.Id });
        }

        private async Task LoadMealsAsync(IEnumerable<int>? selectedIds = null)
        {
            ViewBag.Meals = await _db.Meals.Where(m => m.IsActive).OrderBy(m => m.Name).ToListAsync();
            ViewBag.SelectedMealIds = selectedIds?.ToHashSet() ?? new HashSet<int>();
        }
    }
}