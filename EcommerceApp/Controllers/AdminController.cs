using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EcommerceApp.Data;
using EcommerceApp.Models;
using EcommerceApp.Services;

namespace EcommerceApp.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        IWebHostEnvironment env,
        SupabaseStorageService storage) : Controller
    {
        public async Task<IActionResult> Index()
        {
            var products = await context.Products.AsNoTracking().ToListAsync();

            var generoCounts = new Dictionary<string, int>
            {
                ["Masculino"] = products.Count(p => p.Genero == "Masculino"),
                ["Femenino"] = products.Count(p => p.Genero == "Femenino"),
                ["Unisex"] = products.Count(p => p.Genero == "Unisex"),
                ["Sin especificar"] = products.Count(p => string.IsNullOrWhiteSpace(p.Genero))
            };

            var tallasPosibles = new[] { "S", "M", "L", "XL" };
            var tallaCounts = tallasPosibles.ToDictionary(
                talla => talla,
                talla => products.Count(p => (p.Tallas ?? "")
                    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Contains(talla)));

            var viewModel = new AdminDashboardViewModel
            {
                TotalProducts = products.Count,
                TotalStock = products.Sum(p => p.Stock),
                InventoryValue = products.Sum(p => p.Price * p.Stock),
                LowStockCount = products.Count(p => p.Stock <= 5),
                TotalUsers = await userManager.Users.CountAsync(),
                PendingCustomOrders = await context.CustomOrderRequests.CountAsync(p => p.Estado == "Pendiente"),
                PendingOrders = await context.Orders.CountAsync(o => o.Status == "Pendiente"),
                PendingSerigrafiado = await context.SerigrafiadoRequests.CountAsync(s => s.Estado == "Pendiente"),
                RecentProducts = products
                    .OrderByDescending(p => p.CreatedAt)
                    .Take(6)
                    .ToList(),
                GeneroCounts = generoCounts,
                TallaCounts = tallaCounts
            };

            return View(viewModel);
        }

        // Muestra cuántas imágenes locales hay pendientes de migrar, antes de tocar nada.
        [HttpGet]
        public async Task<IActionResult> MigrarImagenes()
        {
            var products = await context.Products.AsNoTracking().ToListAsync();

            var pendientes = products.SelectMany(p => new[]
                {
                    (Producto: p.Name, Campo: "Principal", Url: p.ImageUrl),
                    (Producto: p.Name, Campo: "Frente", Url: p.ImageUrlFrente),
                    (Producto: p.Name, Campo: "Espalda", Url: p.ImageUrlEspalda)
                })
                .Where(x => !string.IsNullOrWhiteSpace(x.Url) && !x.Url!.StartsWith("http"))
                .ToList();

            ViewBag.TotalPendientes = pendientes.Count;
            return View(pendientes);
        }

        // Sube a Supabase cada imagen que todavía sea una ruta local (wwwroot)
        // y actualiza el producto con la nueva URL pública. No modifica las
        // imágenes que ya sean una URL http(s) (esas ya están en la nube).
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MigrarImagenesConfirmado()
        {
            var products = await context.Products.ToListAsync();

            var migradas = 0;
            var noEncontradas = new List<string>();
            var errores = new List<string>();

            async Task<string?> MigrarCampoAsync(string? url)
            {
                if (string.IsNullOrWhiteSpace(url) || url.StartsWith("http"))
                    return url; // ya está en la nube, o no tiene imagen

                var rutaRelativa = url.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
                var rutaFisica = Path.Combine(env.WebRootPath, rutaRelativa);

                if (!System.IO.File.Exists(rutaFisica))
                {
                    noEncontradas.Add(url);
                    return url; // dejamos el valor como estaba
                }

                try
                {
                    await using var stream = System.IO.File.OpenRead(rutaFisica);
                    var contentType = Path.GetExtension(rutaFisica).ToLowerInvariant() switch
                    {
                        ".png" => "image/png",
                        ".webp" => "image/webp",
                        _ => "image/jpeg"
                    };

                    var nuevaUrl = await storage.UploadAsync(stream, Path.GetFileName(rutaFisica), contentType);
                    migradas++;
                    return nuevaUrl;
                }
                catch (Exception ex)
                {
                    errores.Add($"{url}: {ex.Message}");
                    return url;
                }
            }

            foreach (var product in products)
            {
                product.ImageUrl = await MigrarCampoAsync(product.ImageUrl);
                product.ImageUrlFrente = await MigrarCampoAsync(product.ImageUrlFrente);
                product.ImageUrlEspalda = await MigrarCampoAsync(product.ImageUrlEspalda);
            }

            await context.SaveChangesAsync();

            TempData["MigracionResultado"] =
                $"Se migraron {migradas} imagen(es) a Supabase. " +
                $"No encontradas: {noEncontradas.Count}. Errores: {errores.Count}.";

            if (errores.Count > 0)
                TempData["MigracionErrores"] = string.Join(" | ", errores.Take(10));

            return RedirectToAction(nameof(MigrarImagenes));
        }
    }
}
