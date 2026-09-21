using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EcommerceApp.Data;
using EcommerceApp.Models;
using EcommerceApp.Services;

namespace EcommerceApp.Controllers
{
    // Cotizaciones de serigrafía/estampado: el cliente elige qué prendas
    // quiere, en qué tela, cuántas y describe el diseño. El administrador
    // revisa el pedido y contacta al cliente con la tela y el costo.
    [Authorize]
    public class SerigrafiadoController(ApplicationDbContext context, SupabaseStorageService storage, IConfiguration configuration) : Controller
    {
        private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        private string WhatsAppNumber => configuration["Contacto:WhatsApp"] ?? "";

        [HttpGet]
        public IActionResult Nuevo()
        {
            return View(new SerigrafiadoRequest());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Nuevo(SerigrafiadoRequest model, IFormFile? imagen, string[]? prendas)
        {
            ModelState.Remove(nameof(SerigrafiadoRequest.UserId));
            ModelState.Remove(nameof(SerigrafiadoRequest.Estado));
            ModelState.Remove(nameof(SerigrafiadoRequest.TiposPrenda));

            model.TiposPrenda = (prendas != null && prendas.Length > 0) ? string.Join(",", prendas) : "";
            if (string.IsNullOrEmpty(model.TiposPrenda))
                ModelState.AddModelError(nameof(SerigrafiadoRequest.TiposPrenda), "Elige al menos un tipo de prenda");

            if (!ModelState.IsValid) return View(model);

            model.UserId = UserId;
            model.Estado = "Pendiente";
            model.CreatedAt = DateTime.UtcNow;

            if (imagen != null && imagen.Length > 0)
            {
                await using var stream = imagen.OpenReadStream();
                model.ImagenReferenciaUrl = await storage.UploadAsync(stream, imagen.FileName, imagen.ContentType);
            }

            context.SerigrafiadoRequests.Add(model);
            await context.SaveChangesAsync();

            return RedirectToAction(nameof(Confirmacion), new { id = model.Id });
        }

        [HttpGet]
        public async Task<IActionResult> Confirmacion(int id)
        {
            var pedido = await context.SerigrafiadoRequests
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
            var pedidos = await context.SerigrafiadoRequests
                .AsNoTracking()
                .Where(p => p.UserId == UserId)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            return View(pedidos);
        }

        private string BuildWhatsAppLink(SerigrafiadoRequest pedido)
        {
            if (string.IsNullOrWhiteSpace(WhatsAppNumber)) return "#";

            var mensaje = $"Hola, quiero coordinar mi cotización de serigrafía #{pedido.Id} " +
                          $"({pedido.Cantidad} unidad(es) de {pedido.TiposPrenda.Replace(",", ", ")}): {pedido.Descripcion}";

            return $"https://wa.me/{WhatsAppNumber}?text={Uri.EscapeDataString(mensaje)}";
        }

        // ---------- ADMIN ----------

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Admin()
        {
            var pedidos = await context.SerigrafiadoRequests
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
            var pedido = await context.SerigrafiadoRequests.FindAsync(id);
            var estadosValidos = new[] { "Pendiente", "Cotizado", "Confirmado", "Completado" };
            if (pedido != null && estadosValidos.Contains(estado))
            {
                pedido.Estado = estado;
                await context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Admin));
        }
    }
}
