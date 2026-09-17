using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using EcommerceApp.Data;
using EcommerceApp.Models;
using System.Globalization;

// Fija el formato de números en todo el sitio (punto decimal, ej: 150.00),
// para que coincida con la validación del navegador y evitar que el
// formulario de Editar/Crear producto rechace precios válidos.
var cultura = new CultureInfo("en-US");
CultureInfo.DefaultThreadCurrentCulture = cultura;
CultureInfo.DefaultThreadCurrentUICulture = cultura;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequiredLength = 6;
})
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.ExpireTimeSpan = TimeSpan.FromMinutes(60);
    options.SlidingExpiration = true;
});

builder.Services.AddAuthentication()
    .AddGoogle(options =>
    {
        options.ClientId = builder.Configuration["Authentication:Google:ClientId"]!;
        options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"]!;
    });

builder.Services.AddControllersWithViews();

var app = builder.Build();

app.UseRequestLocalization(new Microsoft.AspNetCore.Builder.RequestLocalizationOptions
{
    DefaultRequestCulture = new Microsoft.AspNetCore.Localization.RequestCulture(cultura),
    SupportedCultures = new[] { cultura },
    SupportedUICultures = new[] { cultura }
});

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}/{id?}");

// Crear la base de datos y las tablas automáticamente si no existen (sin migraciones)
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    context.Database.EnsureCreated();
}

// Crear roles por defecto solo si no existen (evita errores al reiniciar la app)
using (var scope = app.Services.CreateScope())
{
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    string[] roles = { "Admin", "User" };
    foreach (var role in roles)
    {
        if (!await roleManager.RoleExistsAsync(role))
            await roleManager.CreateAsync(new IdentityRole(role));
    }
}

// Sembrar productos de ejemplo si la tabla está vacía
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

    if (!context.Products.Any())
    {
        string Img(string categoria, string texto) =>
            $"https://placehold.co/600x600/0b0b0d/f4f5f7?text={Uri.EscapeDataString(categoria)}%0A{Uri.EscapeDataString(texto)}";

        var sample = new List<Product>
        {
            // ---- Vóley ----
            new() { Name = "Camiseta Vóley Attack", Description = "Camiseta de juego corte ágil, tela transpirable, ideal para entrenamiento y partido.", Price = 189.90m, Stock = 18, Category = "Voley", ImageUrl = Img("VOLEY", "Attack") },
            new() { Name = "Camiseta Vóley Spike Pro", Description = "Tela de secado rápido con panel de ventilación en la espalda.", Price = 199.90m, Stock = 12, Category = "Voley", ImageUrl = Img("VOLEY", "Spike Pro") },
            new() { Name = "Short Vóley Rally", Description = "Short liviano con elástico ajustable y forro interior.", Price = 99.90m, Stock = 25, Category = "Voley", ImageUrl = Img("VOLEY", "Rally") },
            new() { Name = "Camiseta Vóley Líbero", Description = "Corte diferenciado para el jugador líbero, colores contrastantes.", Price = 209.90m, Stock = 6, Category = "Voley", ImageUrl = Img("VOLEY", "Libero") },
            new() { Name = "Conjunto Vóley Match", Description = "Camiseta + short a juego, ideal para uniformar equipos completos.", Price = 329.90m, Stock = 10, Category = "Voley", ImageUrl = Img("VOLEY", "Match") },
            new() { Name = "Camiseta Vóley Block", Description = "Refuerzo en hombros para mayor durabilidad en el bloqueo.", Price = 194.90m, Stock = 4, Category = "Voley", ImageUrl = Img("VOLEY", "Block") },
            new() { Name = "Short Vóley Set", Description = "Short deportivo con bolsillo interno para llave o celular.", Price = 105.90m, Stock = 20, Category = "Voley", ImageUrl = Img("VOLEY", "Set") },
            new() { Name = "Camiseta Vóley Ace Elite", Description = "Línea premium con estampado sublimado de alta durabilidad.", Price = 249.90m, Stock = 8, Category = "Voley", ImageUrl = Img("VOLEY", "Ace Elite") },

            // ---- Fútbol ----
            new() { Name = "Camiseta Fútbol Titular", Description = "Camiseta oficial de local, tela dry-fit, escudo bordado.", Price = 219.90m, Stock = 22, Category = "Futbol", ImageUrl = Img("FUTBOL", "Titular") },
            new() { Name = "Camiseta Fútbol Visitante", Description = "Segunda equipación, diseño alternativo del club.", Price = 219.90m, Stock = 15, Category = "Futbol", ImageUrl = Img("FUTBOL", "Visitante") },
            new() { Name = "Short Fútbol Match", Description = "Short de juego con forro interior y elástico reforzado.", Price = 109.90m, Stock = 30, Category = "Futbol", ImageUrl = Img("FUTBOL", "Match") },
            new() { Name = "Camiseta Fútbol Portero", Description = "Diseño acolchado en codos, colores diferenciados del equipo.", Price = 229.90m, Stock = 5, Category = "Futbol", ImageUrl = Img("FUTBOL", "Portero") },
            new() { Name = "Conjunto Fútbol Local", Description = "Camiseta + short + medias, set completo para el equipo.", Price = 389.90m, Stock = 9, Category = "Futbol", ImageUrl = Img("FUTBOL", "Local") },
            new() { Name = "Camiseta Fútbol Retro", Description = "Edición conmemorativa inspirada en modelos clásicos.", Price = 259.90m, Stock = 3, Category = "Futbol", ImageUrl = Img("FUTBOL", "Retro") },
            new() { Name = "Short Fútbol Training", Description = "Short de entrenamiento, tela ligera y bolsillos laterales.", Price = 89.90m, Stock = 28, Category = "Futbol", ImageUrl = Img("FUTBOL", "Training") },
            new() { Name = "Camiseta Fútbol Edición Especial", Description = "Estampado exclusivo en edición limitada de temporada.", Price = 279.90m, Stock = 6, Category = "Futbol", ImageUrl = Img("FUTBOL", "Edicion Especial") },

            // ---- Basquetbol ----
            new() { Name = "Camiseta Basquetbol Home", Description = "Camiseta sin mangas, tela ligera con malla de ventilación.", Price = 179.90m, Stock = 16, Category = "Basquetbol", ImageUrl = Img("BASQUETBOL", "Home") },
            new() { Name = "Camiseta Basquetbol Away", Description = "Versión visitante, mismo corte y calidad que la titular.", Price = 179.90m, Stock = 14, Category = "Basquetbol", ImageUrl = Img("BASQUETBOL", "Away") },
            new() { Name = "Short Basquetbol Pro", Description = "Short amplio de cancha con cintura elástica ajustable.", Price = 119.90m, Stock = 20, Category = "Basquetbol", ImageUrl = Img("BASQUETBOL", "Pro") },
            new() { Name = "Camiseta Basquetbol Sin Mangas", Description = "Corte clásico sin mangas, numeración lista para estampar.", Price = 169.90m, Stock = 7, Category = "Basquetbol", ImageUrl = Img("BASQUETBOL", "Sin Mangas") },
            new() { Name = "Conjunto Basquetbol Team", Description = "Camiseta + short a juego para uniformar equipos completos.", Price = 289.90m, Stock = 11, Category = "Basquetbol", ImageUrl = Img("BASQUETBOL", "Team") },
            new() { Name = "Camiseta Basquetbol Retro", Description = "Edición retro inspirada en uniformes clásicos de la NBA.", Price = 229.90m, Stock = 2, Category = "Basquetbol", ImageUrl = Img("BASQUETBOL", "Retro") },
            new() { Name = "Short Basquetbol Training", Description = "Short liviano ideal para entrenamientos diarios.", Price = 99.90m, Stock = 24, Category = "Basquetbol", ImageUrl = Img("BASQUETBOL", "Training") },
            new() { Name = "Camiseta Basquetbol Edición Limitada", Description = "Estampado especial en tiraje limitado de la temporada.", Price = 259.90m, Stock = 5, Category = "Basquetbol", ImageUrl = Img("BASQUETBOL", "Edicion Limitada") },
        };

        context.Products.AddRange(sample);
        context.SaveChanges();
    }
}

app.Run();
