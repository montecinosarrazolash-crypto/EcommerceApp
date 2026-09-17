using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EcommerceApp.Data;
using EcommerceApp.Models;

namespace EcommerceApp.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController(ApplicationDbContext context, UserManager<ApplicationUser> userManager) : Controller
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
                RecentProducts = products
                    .OrderByDescending(p => p.CreatedAt)
                    .Take(6)
                    .ToList(),
                GeneroCounts = generoCounts,
                TallaCounts = tallaCounts
            };

            return View(viewModel);
        }
    }
}
