using EcommerceApp.Data;
using EcommerceApp.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace EcommerceApp.Controllers
{
    public class HomeController(ApplicationDbContext context) : Controller
    {
        public async Task<IActionResult> Index()
        {
            // Vitrina de destacados: los productos activos con stock más
            // recientes, para que la portada nunca se vea vacía ni estática.
            var destacados = await context.Products
                .AsNoTracking()
                .Where(p => p.IsActive && p.Stock > 0)
                .OrderByDescending(p => p.CreatedAt)
                .Take(8)
                .ToListAsync();

            return View(destacados);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
