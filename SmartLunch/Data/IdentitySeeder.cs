using Microsoft.AspNetCore.Identity;
using SmartLunch.Models;

namespace SmartLunch.Data
{
    public static class IdentitySeeder
    {
        public const string ParentRole = "Parent";
        public const string AdministratorRole = "Administrator";

        public static async Task SeedRolesAsync(IServiceProvider services)
        {
            var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();

            foreach (var roleName in new[] { ParentRole, AdministratorRole })
            {
                if (!await roleManager.RoleExistsAsync(roleName))
                {
                    await roleManager.CreateAsync(new IdentityRole(roleName));
                }
            }
        }

        public static async Task SeedUsersAsync(IServiceProvider services)
        {
            var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

            await CreateUserAsync(userManager, "admin@smartlunch.test", "Admin", "SmartLunch", AdministratorRole);
            await CreateUserAsync(userManager, "parent1@smartlunch.test", "Marie", "Tremblay", ParentRole);
            await CreateUserAsync(userManager, "parent2@smartlunch.test", "Luc", "Dubois", ParentRole);
        }

        private static async Task CreateUserAsync(
            UserManager<ApplicationUser> userManager,
            string email,
            string firstName,
            string lastName,
            string role)
        {
            // Si le compte existe déjà, on ne le recrée pas
            if (await userManager.FindByEmailAsync(email) != null)
            {
                return;
            }

            var user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true, // obligatoire : Program.cs exige un compte confirmé
                FirstName = firstName,
                LastName = lastName
            };

            // Mot de passe de TEST seulement (respecte les règles d'Identity)
            var result = await userManager.CreateAsync(user, "Test123!");

            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                throw new InvalidOperationException($"Impossible de créer le compte {email} : {errors}");
            }

            var roleResult = await userManager.AddToRoleAsync(user, role);

            if (!roleResult.Succeeded)
            {
                var errors = string.Join(", ", roleResult.Errors.Select(e => e.Description));
                throw new InvalidOperationException($"Impossible d'ajouter {email} au rôle {role} : {errors}");
            }
        }
    }
}

/*
Les trois comptes ont le même mot de passe de test : Test123!. Les adresses sont admin@smartlunch.test, parent1@smartlunch.test et parent2@smartlunch.test. Note-les pour le README et pour Doris.
EmailConfirmed = true est indispensable, sinon la connexion sera refusée.
Le test FindByEmailAsync évite de créer les comptes en double à chaque démarrage.
 */