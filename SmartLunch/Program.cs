using System.Globalization;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SmartLunch.Data;
using SmartLunch.Models;
using SmartLunch.Services;
var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddDefaultIdentity<ApplicationUser>(options => options.SignIn.RequireConfirmedAccount = true)
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();
builder.Services.AddControllersWithViews();

builder.Services.AddScoped<ICreditService, CreditService>();
builder.Services.AddScoped<IOrderService, OrderService>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    // Les rôles sont nécessaires dans tous les environnements
    await IdentitySeeder.SeedRolesAsync(scope.ServiceProvider);

    // Les comptes de test et les données de démo seulement en développement
    if (app.Environment.IsDevelopment())
    {
        await IdentitySeeder.SeedUsersAsync(scope.ServiceProvider);
        await DbSeeder.SeedAsync(scope.ServiceProvider);
    }
}

// Culture fr-CA pour tout le site : dates en français, prix en « 6,75 $ »
var frCA = new CultureInfo("fr-CA");
frCA.NumberFormat.NumberDecimalSeparator = ".";    // les formulaires envoient 7.25
frCA.NumberFormat.CurrencyDecimalSeparator = ",";  // affichage : 7,25 $

app.UseRequestLocalization(new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture(frCA),
    SupportedCultures = new[] { frCA },
    SupportedUICultures = new[] { frCA }
});

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.MapRazorPages()
   .WithStaticAssets();

app.Run();
