using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EcommerceApp.Data;

namespace EcommerceApp.Controllers
{
    // Pedidos ya confirmados por el cliente (Status distinto de "Carrito").
    [Authorize]
    public class OrdersController(ApplicationDbContext context) : Controller
    {
        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

            var orders = await context.Orders
                .Include(o => o.Items)
                .Where(o => o.UserId == userId && o.Status != "Carrito")
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();

            return View(orders);
        }

        // ---------- ADMIN ----------

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Admin()
        {
            var orders = await context.Orders
                .Include(o => o.Items)
                .Include(o => o.User)
                .Where(o => o.Status != "Carrito")
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();

            return View(orders);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CambiarEstado(int id, string estado)
        {
            var order = await context.Orders.FindAsync(id);
            if (order != null && (estado == "Pendiente" || estado == "Completado" || estado == "Cancelado"))
            {
                order.Status = estado;
                order.UpdatedAt = DateTime.UtcNow;
                await context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Admin));
        }
    }
}
