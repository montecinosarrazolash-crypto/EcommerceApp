using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EcommerceApp.Data;
using EcommerceApp.Models;

namespace EcommerceApp.Controllers
{
    // El "carrito" es un Order con Status = "Carrito". Al confirmar,
    // pasa a Status = "Pendiente" y se convierte en un pedido real
    // (visible en Mis pedidos / OrdersController).
    [Authorize]
    public class CartController(ApplicationDbContext context) : Controller
    {
        private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        private async Task<Order> GetOrCreateCartAsync()
        {
            var cart = await context.Orders
                .Include(o => o.Items)
                .FirstOrDefaultAsync(o => o.UserId == UserId && o.Status == "Carrito");

            if (cart == null)
            {
                cart = new Order { UserId = UserId, Status = "Carrito" };
                context.Orders.Add(cart);
                await context.SaveChangesAsync();
            }

            return cart;
        }

        public async Task<IActionResult> Index()
        {
            var cart = await GetOrCreateCartAsync();

            // Recargar con los productos incluidos para mostrar imagen/nombre actualizados
            cart = await context.Orders
                .Include(o => o.Items)
                .ThenInclude(i => i.Product)
                .FirstAsync(o => o.Id == cart.Id);

            return View(cart);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add(int productId, int quantity, string? talla, string? genero)
        {
            var product = await context.Products.FindAsync(productId);
            if (product == null) return NotFound();

            quantity = Math.Max(1, quantity);
            talla = string.IsNullOrWhiteSpace(talla) ? null : talla;
            genero = string.IsNullOrWhiteSpace(genero) ? null : genero;

            var cart = await GetOrCreateCartAsync();

            var existing = cart.Items.FirstOrDefault(i =>
                i.ProductId == productId && i.Talla == talla && i.Genero == genero);

            if (existing != null)
            {
                existing.Quantity += quantity;
            }
            else
            {
                context.OrderItems.Add(new OrderItem
                {
                    OrderId = cart.Id,
                    ProductId = product.Id,
                    ProductName = product.Name,
                    UnitPrice = product.Price,
                    Quantity = quantity,
                    Talla = talla,
                    Genero = genero
                });
            }

            await context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateQuantity(int itemId, int quantity)
        {
            var item = await context.OrderItems
                .Include(i => i.Order)
                .FirstOrDefaultAsync(i => i.Id == itemId && i.Order!.UserId == UserId && i.Order.Status == "Carrito");

            if (item == null) return NotFound();

            if (quantity <= 0)
                context.OrderItems.Remove(item);
            else
                item.Quantity = quantity;

            await context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Remove(int itemId)
        {
            var item = await context.OrderItems
                .Include(i => i.Order)
                .FirstOrDefaultAsync(i => i.Id == itemId && i.Order!.UserId == UserId && i.Order.Status == "Carrito");

            if (item != null)
            {
                context.OrderItems.Remove(item);
                await context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Checkout()
        {
            var cart = await GetOrCreateCartAsync();
            if (!cart.Items.Any()) return RedirectToAction(nameof(Index));

            cart.Status = "Pendiente";
            cart.UpdatedAt = DateTime.UtcNow;
            await context.SaveChangesAsync();

            TempData["OrderMessage"] = "¡Tu pedido fue confirmado! Aquí puedes ver su estado.";
            return RedirectToAction("Index", "Orders");
        }
    }
}
