using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SmartLunch.Data;
using SmartLunch.Models;

namespace SmartLunch.Controllers
{
    [Authorize(Roles = IdentitySeeder.ParentRole)]
    public class ContactController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public ContactController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: /Contact — formulaire + historique
        public async Task<IActionResult> Index()
        {
            var vm = new ContactFormViewModel();
            await LoadPageDataAsync(vm);
            return View(vm);
        }

        // POST: /Contact — envoi du message
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(ContactFormViewModel vm)
        {
            var parentId = _userManager.GetUserId(User)!;

            if (ModelState.IsValid)
            {
                // L'enfant choisi doit appartenir au parent connecté
                var child = await _context.Children
                    .FirstOrDefaultAsync(c => c.Id == vm.ChildId && c.ParentId == parentId);

                if (child == null)
                {
                    ModelState.AddModelError(nameof(vm.ChildId), "Cet enfant n'est pas inscrit à votre compte.");
                }
                else
                {
                    //C'est le serveur remplit tout le reste
                    _context.ContactMessages.Add(new ContactMessage
                    {
                        ParentId = parentId,
                        ChildId = child.Id,
                        Message = vm.Message.Trim(),
                        SentAt = DateTime.Now,
                        IsRead = false
                    });
                    await _context.SaveChangesAsync();

                    // La confirmation
                    TempData["Success"] = $"Votre message concernant {child.FirstName} a été envoyé à l'administration.";
                    return RedirectToAction(nameof(Index));
                }
            }

            // Erreur : on réaffiche la page avec la liste et l'historique
            await LoadPageDataAsync(vm);
            return View(vm);
        }

        // Charge la liste des enfants et l'historique, toujours filtrés par parent
        private async Task LoadPageDataAsync(ContactFormViewModel vm)
        {
            var parentId = _userManager.GetUserId(User);

            vm.Children = await _context.Children
                .Where(c => c.ParentId == parentId)
                .OrderBy(c => c.FirstName)
                .Select(c => new SelectListItem
                {
                    Value = c.Id.ToString(),
                    Text = c.FirstName + " " + c.LastName + " (" + c.ClassNumber + ")"
                })
                .ToListAsync();

            vm.SentMessages = await _context.ContactMessages
                .Where(m => m.ParentId == parentId)
                .Include(m => m.Child)
                .OrderByDescending(m => m.SentAt)
                .ToListAsync();
        }
    }
}