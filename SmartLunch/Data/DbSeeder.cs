using Microsoft.EntityFrameworkCore;
using SmartLunch.Models;

namespace SmartLunch.Data
{
    public static class DbSeeder
    {
        public static async Task SeedAsync(IServiceProvider services)
        {
            var db = services.GetRequiredService<ApplicationDbContext>();

            // Si la base contient déjà des données, on ne fait rien (pas de doublons)
            if (await db.Allergies.AnyAsync() || await db.Meals.AnyAsync() || await db.DailyMenus.AnyAsync())
            {
                return;
            }

            // --- 1. Allergies ---
            var allergyNames = new[] { "Arachides", "Produits laitiers", "Gluten", "Œufs", "Poisson", "Soja" };
            var allergies = allergyNames.ToDictionary(n => n, n => new Allergy { Name = n });
            db.Allergies.AddRange(allergies.Values);

            // --- 2. Repas (15, pour pouvoir varier les menus) ---
            var mealData = new (string Name, string Description, decimal Price, bool Vegetarian, string[] Allergens)[]
            {
                ("Poulet et riz", "Poulet grillé, riz et légumes", 7.25m, false, Array.Empty<string>()),
                ("Macaroni au fromage", "Macaroni gratinés au fromage", 6.75m, true, new[] { "Gluten", "Produits laitiers" }),
                ("Pâtes sauce Alfredo", "Pâtes et sauce Alfredo crémeuse", 7.00m, true, new[] { "Gluten", "Produits laitiers" }),
                ("Lasagne maison", "Lasagne à la viande et au fromage", 7.50m, false, new[] { "Gluten", "Produits laitiers", "Œufs" }),
                ("Pizza pochette", "Pochette de pizza au fromage et pepperoni", 7.00m, false, new[] { "Gluten", "Produits laitiers" }),
                ("Burger de bœuf", "Burger de bœuf avec salade", 7.25m, false, new[] { "Gluten" }),
                ("Doigts de poulet", "Doigts de poulet au blé entier", 7.00m, false, new[] { "Gluten", "Œufs" }),
                ("Filet de poisson pané", "Filet de poisson pané et pommes de terre", 7.50m, false, new[] { "Poisson", "Gluten" }),
                ("Spaghetti bolognaise", "Spaghetti et sauce à la viande", 7.25m, false, new[] { "Gluten" }),
                ("Soupe-repas au poulet", "Soupe au poulet et aux nouilles", 6.50m, false, new[] { "Gluten", "Œufs" }),
                ("Salade César au poulet", "Salade César avec poulet grillé", 7.00m, false, new[] { "Œufs", "Produits laitiers" }),
                ("Poulet sauté aux arachides", "Poulet sauté, sauce aux arachides et riz", 7.50m, false, new[] { "Arachides", "Soja" }),
                ("Boulettes végé", "Boulettes végétariennes, sauce tomate", 6.75m, true, new[] { "Soja", "Gluten" }),
                ("Crêpes jambon et fromage", "Crêpes salées au jambon et au fromage", 6.75m, false, new[] { "Gluten", "Œufs", "Produits laitiers" }),
                ("Sauté de légumes et tofu", "Légumes sautés, tofu et riz", 6.75m, true, new[] { "Soja" })
            };

            var meals = mealData.Select(m => new Meal
            {
                Name = m.Name,
                Description = m.Description,
                Price = m.Price,
                IsVegetarian = m.Vegetarian,
                IsActive = true,
                MealAllergies = m.Allergens
                    .Select(a => new MealAllergy { Allergy = allergies[a] })
                    .ToList()
            }).ToList();
            db.Meals.AddRange(meals);

            // --- 3. Menus : du 16 novembre au 18 décembre 2026, jours de semaine seulement ---
            var closedDays = new Dictionary<DateOnly, string>
            {
                [new DateOnly(2026, 11, 20)] = "Journée pédagogique",
                [new DateOnly(2026, 12, 4)] = "Journée pédagogique"
            };

            var openDayIndex = 0;
            for (var date = new DateOnly(2026, 11, 16); date <= new DateOnly(2026, 12, 18); date = date.AddDays(1))
            {
                if (date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday)
                {
                    continue;
                }

                var menu = new DailyMenu { Date = date };

                if (closedDays.TryGetValue(date, out var reason))
                {
                    // Journée fermée : aucun repas
                    menu.IsClosed = true;
                    menu.ClosedReason = reason;
                }
                else
                {
                    // Journée ouverte : exactement 3 repas différents (rotation sur les 15 repas)
                    for (var k = 0; k < 3; k++)
                    {
                        var meal = meals[(openDayIndex * 3 + k) % meals.Count];
                        menu.DailyMenuMeals.Add(new DailyMenuMeal { Meal = meal });
                    }
                    openDayIndex++;
                }

                db.DailyMenus.Add(menu);
            }

            // Un seul SaveChanges : tout est enregistré ensemble, ou rien
            await db.SaveChangesAsync();
        }
    }
}