using Microsoft.AspNetCore.Identity;

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
    }
}