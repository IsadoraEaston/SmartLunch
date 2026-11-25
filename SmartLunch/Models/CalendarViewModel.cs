using Microsoft.AspNetCore.Mvc.Rendering;

namespace SmartLunch.Models
{
    // Les 4 types de jours
    public enum CalendarDayType
    {
        NoMenu,   // aucun menu prévu ce jour-là
        Closed,   // journée fermée (pédagogique, tempête…)
        Locked,   // jour passé, ou aujourd'hui après 08 h 30
        Open      // on peut encore commander
    }

    // Un repas dans une case de jour
    public class CalendarMealViewModel
    {
        public int MealId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public decimal Price { get; set; }
        public bool IsVegetarian { get; set; }
        public bool IsFavorite { get; set; }    // ❤️ dans FavoriteMeals pour cet enfant
        public bool IsPopular { get; set; }     // 😊 parmi les 3 repas les plus commandés
        public List<string> AllergyWarnings { get; set; } = new();  // ⚠️ contrainte 4
    }

    // Une case du calendrier
    public class CalendarDayViewModel
    {
        public DateOnly Date { get; set; }
        public CalendarDayType Type { get; set; }
        public string? ClosedReason { get; set; }
        public List<CalendarMealViewModel> Meals { get; set; } = new();

        // La commande déjà passée pour ce jour (null s'il n'y en a pas)
        public int? OrderedMealId { get; set; }
        public bool OrderedExtraPortion { get; set; }
        public decimal? OrderedTotal { get; set; }
    }

    // Toute la page
    public class CalendarViewModel
    {
        public int ChildId { get; set; }
        public string ChildName { get; set; } = string.Empty;
        public List<SelectListItem> Children { get; set; } = new();   // ta Q1 : choisir l'enfant

        public int Year { get; set; }
        public int Month { get; set; }
        public decimal ExtraPortionFee { get; set; }

        // Les semaines du mois : chaque semaine = 5 cases (lundi → vendredi).
        // Une case vaut null si le jour n'appartient pas au mois affiché.
        public List<List<CalendarDayViewModel?>> Weeks { get; set; } = new();
    }
}