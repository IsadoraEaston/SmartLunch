using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartLunch.Data;
using SmartLunch.Models;

namespace SmartLunch.Controllers.Admin
{
    [Authorize(Roles = IdentitySeeder.AdministratorRole)]
    [Route("Admin/Meals")]
    public class MealsController : Controller
    {
        private readonly ApplicationDbContext _db;

        public MealsController(ApplicationDbContext db)
        {
            _db = db;
        }

        // GET: Admin/Meals
        [HttpGet("")]
        public async Task<IActionResult> Index()
        {
            var meals = await _db.Meals
                .Include(m => m.MealAllergies).ThenInclude(ma => ma.Allergy)
                .OrderBy(m => m.Name)
                .ToListAsync();
            return View("~/Views/Admin/Meals/Index.cshtml", meals);
        }

        // GET: Admin/Meals/Create
        [HttpGet("Create")]
        public async Task<IActionResult> Create()
        {
            await LoadAllergiesAsync();
            return View("~/Views/Admin/Meals/Create.cshtml", new Meal());
        }

        // POST: Admin/Meals/Create
        [HttpPost("Create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Meal meal, int[] allergyIds)
        {
            // Les collections de navigation ne viennent pas du formulaire
            ModelState.Remove(nameof(Meal.DailyMenuMeals));
            ModelState.Remove(nameof(Meal.MealAllergies));
            ModelState.Remove(nameof(Meal.FavoriteMeals));
            ModelState.Remove(nameof(Meal.Orders));

            // Garde seulement les allergies qui existent vraiment
            var validIds = await ValidAllergyIdsAsync(allergyIds);

            if (!ModelState.IsValid)
            {
                await LoadAllergiesAsync(validIds);
                return View("~/Views/Admin/Meals/Create.cshtml", meal);
            }

            meal.IsActive = true;
            meal.MealAllergies = validIds
                .Select(id => new MealAllergy { AllergyId = id })
                .ToList();

            _db.Meals.Add(meal);
            await _db.SaveChangesAsync();

            TempData["Success"] = $"Le repas « {meal.Name} » a été ajouté.";
            return RedirectToAction(nameof(Index));
        }

        // GET: Admin/Meals/Edit/5
        [HttpGet("Edit/{id:int}")]
        public async Task<IActionResult> Edit(int id)
        {
            var meal = await _db.Meals
                .Include(m => m.MealAllergies)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (meal == null) return NotFound();

            await LoadAllergiesAsync(meal.MealAllergies.Select(ma => ma.AllergyId));
            return View("~/Views/Admin/Meals/Edit.cshtml", meal);
        }

        // POST: Admin/Meals/Edit/5
        [HttpPost("Edit/{id:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Meal input, int[] allergyIds)
        {
            ModelState.Remove(nameof(Meal.DailyMenuMeals));
            ModelState.Remove(nameof(Meal.MealAllergies));
            ModelState.Remove(nameof(Meal.FavoriteMeals));
            ModelState.Remove(nameof(Meal.Orders));

            // Sans Include : on ne suit pas les anciens MealAllergy
            var meal = await _db.Meals.FirstOrDefaultAsync(m => m.Id == id);
            if (meal == null) return NotFound();

            // Garde seulement les allergies qui existent vraiment
            var validIds = await ValidAllergyIdsAsync(allergyIds);

            if (!ModelState.IsValid)
            {
                input.Id = id;
                await LoadAllergiesAsync(validIds);
                return View("~/Views/Admin/Meals/Edit.cshtml", input);
            }

            // On copie seulement les champs autorisés (jamais l'objet complet du formulaire)
            meal.Name = input.Name;
            meal.Description = input.Description;
            meal.Price = input.Price;
            meal.IsVegetarian = input.IsVegetarian;
            meal.IsActive = input.IsActive;

            // Remplace les allergènes : suppression directe en base, puis ajout des nouveaux
            await _db.MealAllergies.Where(ma => ma.MealId == id).ExecuteDeleteAsync();
            foreach (var aid in validIds)
            {
                _db.MealAllergies.Add(new MealAllergy { MealId = id, AllergyId = aid });
            }

            await _db.SaveChangesAsync();

            TempData["Success"] = $"Le repas « {meal.Name} » a été modifié.";
            return RedirectToAction(nameof(Index));
        }

        // POST: Admin/Meals/Delete/5
        [HttpPost("Delete/{id:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var meal = await _db.Meals.FirstOrDefaultAsync(m => m.Id == id);
            if (meal == null) return NotFound();

            var hasOrders = await _db.Orders.AnyAsync(o => o.MealId == id);
            var inMenus = await _db.DailyMenuMeals.AnyAsync(d => d.MealId == id);

            if (hasOrders || inMenus)
            {
                // Déjà commandé ou planifié : on le désactive au lieu de le supprimer
                meal.IsActive = false;
                await _db.SaveChangesAsync();
                TempData["Success"] = $"Le repas « {meal.Name} » est utilisé : il a été désactivé au lieu d'être supprimé.";
            }
            else
            {
                await _db.MealAllergies.Where(ma => ma.MealId == id).ExecuteDeleteAsync();
                _db.Meals.Remove(meal);
                await _db.SaveChangesAsync();
                TempData["Success"] = $"Le repas « {meal.Name} » a été supprimé.";
            }

            return RedirectToAction(nameof(Index));
        }

        // Ignore les Id inventés : garde seulement les allergies qui existent
        private async Task<List<int>> ValidAllergyIdsAsync(int[] allergyIds)
        {
            var ids = allergyIds.Distinct().ToList();
            return await _db.Allergies
                .Where(a => ids.Contains(a.Id))
                .Select(a => a.Id)
                .ToListAsync();
        }

        private async Task LoadAllergiesAsync(IEnumerable<int>? selectedIds = null)
        {
            ViewBag.Allergies = await _db.Allergies.OrderBy(a => a.Name).ToListAsync();
            ViewBag.SelectedAllergyIds = selectedIds?.ToHashSet() ?? new HashSet<int>();
        }
    }
}