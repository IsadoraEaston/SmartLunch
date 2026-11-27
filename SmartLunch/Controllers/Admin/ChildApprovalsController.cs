using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartLunch.Data;
using SmartLunch.Models;

namespace SmartLunch.Controllers.Admin
{
    // Approbation des inscriptions d'enfants (règle de l'enseignante)
    [Authorize(Roles = IdentitySeeder.AdministratorRole)]
    [Route("Admin/Inscriptions")]
    public class ChildApprovalsController : Controller
    {
        private readonly ApplicationDbContext _db;

        public ChildApprovalsController(ApplicationDbContext db)
        {
            _db = db;
        }

        // GET: Admin/Inscriptions — les inscriptions en attente d'abord
        [HttpGet("")]
        public async Task<IActionResult> Index()
        {
            var children = await _db.Children
                .Include(c => c.Parent)
                .Include(c => c.ChildAllergies).ThenInclude(ca => ca.Allergy)
                .OrderBy(c => c.Status)              // Pending (0) avant Approved (1) et Rejected (2)
                .ThenBy(c => c.LastName)
                .ThenBy(c => c.FirstName)
                .ToListAsync();

            return View("~/Views/Admin/ChildApprovals/Index.cshtml", children);
        }

        // POST: Admin/Inscriptions/Approve/5
        [HttpPost("Approve/{id:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(int id)
        {
            var child = await _db.Children.FirstOrDefaultAsync(c => c.Id == id);
            if (child == null) return NotFound();

            child.Status = ChildStatus.Approved;
            child.RejectionReason = null;
            await _db.SaveChangesAsync();

            TempData["Success"] = $"L'inscription de {child.FirstName} {child.LastName} a été approuvée.";
            return RedirectToAction(nameof(Index));
        }

        // POST: Admin/Inscriptions/Reject/5 — la raison est obligatoire (affichée au parent)
        [HttpPost("Reject/{id:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(int id, string? reason)
        {
            var child = await _db.Children.FirstOrDefaultAsync(c => c.Id == id);
            if (child == null) return NotFound();

            reason = reason?.Trim();
            if (string.IsNullOrEmpty(reason))
            {
                TempData["Error"] = $"Indiquez la raison du rejet pour {child.FirstName}.";
                return RedirectToAction(nameof(Index));
            }

            child.Status = ChildStatus.Rejected;
            child.RejectionReason = reason.Length <= 200 ? reason : reason.Substring(0, 200);
            await _db.SaveChangesAsync();

            TempData["Success"] = $"L'inscription de {child.FirstName} {child.LastName} a été rejetée.";
            return RedirectToAction(nameof(Index));
        }
    }
}
