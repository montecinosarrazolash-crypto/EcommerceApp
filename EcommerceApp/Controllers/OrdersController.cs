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
    }
}
