using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartLunch.Data;
using SmartLunch.Models;

namespace SmartLunch.Controllers
{
    // Seuls les parents connectés peuvent accéder à ce contrôleur
    [Authorize(Roles = IdentitySeeder.ParentRole)]
    public class ChildrenController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public ChildrenController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: /Children — la liste des enfants du parent connecté
        public async Task<IActionResult> Index()
        {
            var parentId = _userManager.GetUserId(User);

            var children = await _context.Children
                .Where(c => c.ParentId == parentId)          // contrainte 1 : seulement SES enfants
                .Include(c => c.ChildAllergies)
                    .ThenInclude(ca => ca.Allergy)            // pour afficher le nom des allergies
                .OrderBy(c => c.FirstName)
                .ToListAsync();

            return View(children);
        }


        // GET: /Children/Create — affiche le formulaire vide
        public async Task<IActionResult> Create()
        {
            var vm = new ChildFormViewModel
            {
                AvailableAllergies = await LoadAllergiesAsync()
            };
            return View(vm);
        }

        // POST: /Children/Create — reçoit et enregistre le formulaire
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ChildFormViewModel vm)
        {
            // Si un champ est invalide, on réaffiche le formulaire avec les erreurs
            if (!ModelState.IsValid)
            {
                vm.AvailableAllergies = await LoadAllergiesAsync();
                return View(vm);
            }

            var child = new Child
            {
                FirstName = vm.FirstName.Trim(),
                LastName = vm.LastName.Trim(),
                ClassNumber = vm.ClassNumber.Trim(),
                FoodPreferences = vm.FoodPreferences?.Trim(),
                ParentId = _userManager.GetUserId(User)!   // c'est le SERVEUR qui choisit le parent
            };

            // On ne garde que les allergies qui existent vraiment dans la base
            var validIds = await _context.Allergies
                .Where(a => vm.SelectedAllergyIds.Contains(a.Id))
                .Select(a => a.Id)
                .ToListAsync();

            foreach (var allergyId in validIds)
            {
                child.ChildAllergies.Add(new ChildAllergy { AllergyId = allergyId });
            }

            _context.Children.Add(child);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"{child.FirstName} a été inscrit(e) au service SmartLunch.";
            return RedirectToAction(nameof(Index));
        }

        // Charge toutes les allergies, triées par nom, pour les cases à cocher
        private async Task<List<Allergy>> LoadAllergiesAsync()
        {
            return await _context.Allergies.OrderBy(a => a.Name).ToListAsync();
        }
    }
}