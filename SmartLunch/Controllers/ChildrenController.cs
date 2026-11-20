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
    }
}