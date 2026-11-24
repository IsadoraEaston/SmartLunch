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
                ClassNumber = vm.ClassNumber.Trim().ToUpper(),
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


        // GET: /Children/Edit/5 — formulaire pré-rempli (ta Q3)
        public async Task<IActionResult> Edit(int id)
        {
            var parentId = _userManager.GetUserId(User);

            // Ta Q2 : l'enfant doit avoir CET Id ET appartenir à CE parent
            var child = await _context.Children
                .Include(c => c.ChildAllergies)
                .FirstOrDefaultAsync(c => c.Id == id && c.ParentId == parentId);

            if (child == null)
            {
                return NotFound();   // pas à lui, ou n'existe pas : même réponse
            }

            // On copie le dossier de l'enfant dans le ViewModel
            var vm = new ChildFormViewModel
            {
                Id = child.Id,
                FirstName = child.FirstName,
                LastName = child.LastName,
                ClassNumber = child.ClassNumber,
                FoodPreferences = child.FoodPreferences,
                SelectedAllergyIds = child.ChildAllergies.Select(ca => ca.AllergyId).ToList(),
                AvailableAllergies = await LoadAllergiesAsync()
            };

            return View(vm);
        }

        // POST: /Children/Edit/5 — enregistre les modifications
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, ChildFormViewModel vm)
        {
            // L'Id de l'adresse et celui du champ caché doivent être identiques
            if (id != vm.Id)
            {
                return BadRequest();
            }

            var parentId = _userManager.GetUserId(User);

            // Ta Q2, une deuxième fois : un tricheur peut envoyer le formulaire directement
            var child = await _context.Children
                .FirstOrDefaultAsync(c => c.Id == id && c.ParentId == parentId);

            if (child == null)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                vm.AvailableAllergies = await LoadAllergiesAsync();
                return View(vm);
            }

            // Ta Q1 : ce que le parent a le droit de modifier
            child.FirstName = vm.FirstName.Trim();
            child.LastName = vm.LastName.Trim();
            child.ClassNumber = vm.ClassNumber.Trim().ToUpper();
            child.FoodPreferences = vm.FoodPreferences?.Trim();

            // Ta Q4, étape 1 : on efface toutes les allergies actuelles
            await _context.ChildAllergies
                .Where(ca => ca.ChildId == child.Id)
                .ExecuteDeleteAsync();

            // Ta Q4, étape 2 : on remplit avec les nouvelles (seulement celles qui existent)
            var validIds = await _context.Allergies
                .Where(a => vm.SelectedAllergyIds.Contains(a.Id))
                .Select(a => a.Id)
                .ToListAsync();

            foreach (var allergyId in validIds)
            {
                _context.ChildAllergies.Add(new ChildAllergy { ChildId = child.Id, AllergyId = allergyId });
            }

            await _context.SaveChangesAsync();

            TempData["Success"] = $"Le dossier de {child.FirstName} a été mis à jour.";
            return RedirectToAction(nameof(Index));
        }

        // Charge toutes les allergies, triées par nom, pour les cases à cocher
        private async Task<List<Allergy>> LoadAllergiesAsync()
        {
            return await _context.Allergies.OrderBy(a => a.Name).ToListAsync();
        }
    }
}