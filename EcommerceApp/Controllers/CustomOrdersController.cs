using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EcommerceApp.Data;
using EcommerceApp.Models;
using EcommerceApp.Services;

namespace EcommerceApp.Controllers
{
    // Pedidos "desde cero": el cliente describe lo que quiere y sube una foto
    // de referencia, en vez de elegir un producto ya cargado en el catálogo.
    [Authorize]
    public class CustomOrdersController(ApplicationDbContext context, SupabaseStorageService storage, IConfiguration configuration) : Controller
    {
        private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        private string WhatsAppNumber => configuration["Contacto:WhatsApp"] ?? "";

        [HttpGet]
        public IActionResult Nuevo()
        {
            return View(new CustomOrderRequest());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Nuevo(CustomOrderRequest model, IFormFile? imagen)
        {
            // Estos dos los ponemos nosotros, no vienen del formulario.
            ModelState.Remove(nameof(CustomOrderRequest.UserId));
            ModelState.Remove(nameof(CustomOrderRequest.Estado));

            if (!ModelState.IsValid) return View(model);

            model.UserId = UserId;
            model.Estado = "Pendiente";
            model.CreatedAt = DateTime.UtcNow;

            if (imagen != null && imagen.Length > 0)
            {
                await using var stream = imagen.OpenReadStream();
                model.ImagenReferenciaUrl = await storage.UploadAsync(stream, imagen.FileName, imagen.ContentType);
            }

            context.CustomOrderRequests.Add(model);
            await context.SaveChangesAsync();

            return RedirectToAction(nameof(Confirmacion), new { id = model.Id });
        }

        [HttpGet]
        public async Task<IActionResult> Confirmacion(int id)
        {
            var pedido = await context.CustomOrderRequests
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == id && p.UserId == UserId);

            if (pedido == null) return RedirectToAction(nameof(Nuevo));

            ViewBag.WhatsAppNumber = WhatsAppNumber;
            ViewBag.WhatsAppLink = BuildWhatsAppLink(pedido);
            return View(pedido);
        }

        [HttpGet]
        public async Task<IActionResult> Mis()
        {
            var pedidos = await context.CustomOrderRequests
                .AsNoTracking()
                .Where(p => p.UserId == UserId)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            return View(pedidos);
        }

        private string BuildWhatsAppLink(CustomOrderRequest pedido)
        {
            if (string.IsNullOrWhiteSpace(WhatsAppNumber)) return "#";

            var mensaje = $"Hola, quiero coordinar mi pedido personalizado #{pedido.Id} " +
                          $"({pedido.Cantidad} unidad(es)): {pedido.Descripcion}";

            return $"https://wa.me/{WhatsAppNumber}?text={Uri.EscapeDataString(mensaje)}";
        }

        // ---------- ADMIN ----------

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Admin()
        {
            var pedidos = await context.CustomOrderRequests
                .Include(p => p.User)
                .AsNoTracking()
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            return View(pedidos);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CambiarEstado(int id, string estado)
        {
            var pedido = await context.CustomOrderRequests.FindAsync(id);
            if (pedido != null && (estado == "Pendiente" || estado == "Contactado" || estado == "Completado"))
            {
                pedido.Estado = estado;
                await context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Admin));
        }
    }
}
