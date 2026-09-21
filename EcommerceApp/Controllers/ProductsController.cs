using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EcommerceApp.Data;
using EcommerceApp.Models;
using EcommerceApp.Services;

namespace EcommerceApp.Controllers
{
    // Constructor primario: "context" reemplaza el campo _context de antes.
    // Sin [Authorize] a nivel de clase: cualquiera puede ver el catálogo y
    // el detalle de un producto sin iniciar sesión. Cada acción de admin
    // (crear/editar/eliminar/gestionar) tiene su propio [Authorize] abajo.
    public class ProductsController(ApplicationDbContext context, SupabaseStorageService storage) : Controller
    {
        // Sube el archivo a Supabase Storage y devuelve la URL pública,
        // lista para guardar en el campo ImageUrl. Si no se subió archivo,
        // devuelve null (y se conserva lo que ya había).
        private async Task<string?> GuardarImagenAsync(IFormFile? archivo)
        {
            if (archivo == null || archivo.Length == 0) return null;

            await using var stream = archivo.OpenReadStream();
            return await storage.UploadAsync(stream, archivo.FileName, archivo.ContentType);
        }

        [AllowAnonymous]
        public async Task<IActionResult> Index(string? category)
        {
            var allProducts = await context.Products.AsNoTracking().Where(p => p.IsActive).ToListAsync();

            var viewModel = new ProductCatalogViewModel { SelectedCategory = category };

            if (!string.IsNullOrWhiteSpace(category))
            {
                viewModel.Sections.Add(new CategorySectionViewModel
                {
                    Category = category,
                    Products = allProducts
                        .Where(p => string.Equals(p.Category, category, StringComparison.OrdinalIgnoreCase))
                        .Take(8)
                        .ToList()
                });
            }
            else
            {
                var categoriesInOrder = new[] { "Voley", "Futbol", "Basquetbol" };

                foreach (var cat in categoriesInOrder)
                {
                    var productsInCategory = allProducts
                        .Where(p => string.Equals(p.Category, cat, StringComparison.OrdinalIgnoreCase))
                        .Take(8)
                        .ToList();
                    if (productsInCategory.Count > 0)
                        viewModel.Sections.Add(new CategorySectionViewModel { Category = cat, Products = productsInCategory });
                }

                var otherProducts = allProducts
                    .Where(p => string.IsNullOrEmpty(p.Category) || !categoriesInOrder.Any(c => string.Equals(c, p.Category, StringComparison.OrdinalIgnoreCase)))
                    .Take(8)
                    .ToList();
                if (otherProducts.Count > 0)
                    viewModel.Sections.Add(new CategorySectionViewModel { Category = "Otros", Products = otherProducts });
            }

            return View(viewModel);
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Manage()
        {
            var products = await context.Products
                .AsNoTracking()
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            return View(products);
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Preview(int id)
        {
            var product = await context.Products.FindAsync(id);
            if (product == null) return NotFound();
            return View(product);
        }

        [AllowAnonymous]
        public async Task<IActionResult> Details(int id)
        {
            var product = await context.Products.FindAsync(id);
            if (product == null) return NotFound();
            return View(product);
        }

        [Authorize(Roles = "Admin")]
        public IActionResult Create() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create(Product product, IFormFile? ArchivoPrincipal, IFormFile? ArchivoFrente, IFormFile? ArchivoEspalda, string[]? TallasSeleccionadas)
        {
            if (!ModelState.IsValid) return View(product);

            product.Tallas = TallasSeleccionadas != null && TallasSeleccionadas.Length > 0
                ? string.Join(",", TallasSeleccionadas)
                : null;

            var urlPrincipal = await GuardarImagenAsync(ArchivoPrincipal);
            if (urlPrincipal != null) product.ImageUrl = urlPrincipal;

            var urlFrente = await GuardarImagenAsync(ArchivoFrente);
            if (urlFrente != null) product.ImageUrlFrente = urlFrente;

            var urlEspalda = await GuardarImagenAsync(ArchivoEspalda);
            if (urlEspalda != null) product.ImageUrlEspalda = urlEspalda;

            context.Products.Add(product);
            await context.SaveChangesAsync();
            return RedirectToAction(nameof(Manage));
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id)
        {
            var product = await context.Products.FindAsync(id);
            if (product == null) return NotFound();
            return View(product);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id, Product product, IFormFile? ArchivoPrincipal, IFormFile? ArchivoFrente, IFormFile? ArchivoEspalda, string[]? TallasSeleccionadas)
        {
            if (id != product.Id) return NotFound();
            if (!ModelState.IsValid) return View(product);

            product.Tallas = TallasSeleccionadas != null && TallasSeleccionadas.Length > 0
                ? string.Join(",", TallasSeleccionadas)
                : null;

            // No confiamos en el CreatedAt que viene del formulario (pierde el
            // "Kind" UTC al pasar por HTML y Postgres lo rechaza). Lo traemos
            // directo de la base de datos.
            var createdAt = await context.Products
                .AsNoTracking()
                .Where(p => p.Id == id)
                .Select(p => p.CreatedAt)
                .FirstOrDefaultAsync();

            product.CreatedAt = DateTime.SpecifyKind(createdAt, DateTimeKind.Utc);
            product.UpdatedAt = DateTime.UtcNow;

            var urlPrincipal = await GuardarImagenAsync(ArchivoPrincipal);
            if (urlPrincipal != null) product.ImageUrl = urlPrincipal;

            var urlFrente = await GuardarImagenAsync(ArchivoFrente);
            if (urlFrente != null) product.ImageUrlFrente = urlFrente;

            var urlEspalda = await GuardarImagenAsync(ArchivoEspalda);
            if (urlEspalda != null) product.ImageUrlEspalda = urlEspalda;

            context.Products.Update(product);
            await context.SaveChangesAsync();
            return RedirectToAction(nameof(Manage));
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var product = await context.Products.FindAsync(id);
            if (product == null) return NotFound();
            return View(product);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var product = await context.Products.FindAsync(id);
            if (product == null) return RedirectToAction(nameof(Manage));

            var tieneVentas = await context.OrderItems.AnyAsync(oi => oi.ProductId == id);

            if (tieneVentas)
            {
                // No se puede borrar sin perder el historial de esos pedidos:
                // lo ocultamos del catálogo en vez de eliminarlo de verdad.
                product.IsActive = false;
                context.Products.Update(product);
                TempData["Mensaje"] = $"\"{product.Name}\" ya tiene pedidos asociados, así que no se puede eliminar por completo. Se ocultó del catálogo.";
            }
            else
            {
                context.Products.Remove(product);
                TempData["Mensaje"] = $"\"{product.Name}\" se eliminó correctamente.";
            }

            await context.SaveChangesAsync();
            return RedirectToAction(nameof(Manage));
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Reactivar(int id)
        {
            var product = await context.Products.FindAsync(id);
            if (product != null)
            {
                product.IsActive = true;
                await context.SaveChangesAsync();
                TempData["Mensaje"] = $"\"{product.Name}\" volvió a estar visible en el catálogo.";
            }
            return RedirectToAction(nameof(Manage));
        }
    }
}
