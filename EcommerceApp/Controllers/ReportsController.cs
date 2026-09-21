using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EcommerceApp.Data;
using EcommerceApp.Models;

namespace EcommerceApp.Controllers
{
    // Reportes descargables en Excel para el administrador: pedidos, pedidos
    // personalizados, cotizaciones de serigrafía e inventario. Todos aceptan
    // un rango de fechas opcional (según la fecha de creación de cada registro).
    [Authorize(Roles = "Admin")]
    public class ReportsController(ApplicationDbContext context) : Controller
    {
        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> DescargarPedidos(DateTime? desde, DateTime? hasta)
        {
            var (from, to) = NormalizarRango(desde, hasta);

            var pedidos = await context.Orders
                .Include(o => o.User)
                .Include(o => o.Items)
                .AsNoTracking()
                .Where(o => o.Status != "Carrito" && o.CreatedAt >= from && o.CreatedAt < to)
                .OrderBy(o => o.CreatedAt)
                .ToListAsync();

            using var workbook = new XLWorkbook();
            var sheet = workbook.Worksheets.Add("Pedidos");

            var headers = new[]
            {
                "N° Pedido", "Fecha", "Cliente", "Usuario", "Teléfono contacto", "Estado",
                "Producto", "Talla", "Género", "Nombre personalizado", "Número personalizado",
                "Cantidad", "Precio unitario (Bs)", "Subtotal (Bs)"
            };
            EscribirEncabezados(sheet, headers);

            var fila = 2;
            foreach (var pedido in pedidos)
            {
                if (pedido.Items.Count == 0)
                {
                    EscribirFilaPedidoBase(sheet, fila, pedido);
                    fila++;
                    continue;
                }

                foreach (var item in pedido.Items)
                {
                    EscribirFilaPedidoBase(sheet, fila, pedido);
                    sheet.Cell(fila, 7).Value = item.ProductName;
                    sheet.Cell(fila, 8).Value = item.Talla ?? "—";
                    sheet.Cell(fila, 9).Value = item.Genero ?? "—";
                    sheet.Cell(fila, 10).Value = item.NombrePersonalizado ?? "—";
                    sheet.Cell(fila, 11).Value = item.NumeroPersonalizado ?? "—";
                    sheet.Cell(fila, 12).Value = item.Quantity;
                    sheet.Cell(fila, 13).Value = item.UnitPrice;
                    sheet.Cell(fila, 14).Value = item.UnitPrice * item.Quantity;
                    fila++;
                }
            }

            AplicarEstiloTabla(sheet, headers.Length, fila - 1);
            if (fila > 2)
            {
                sheet.Range(2, 13, fila - 1, 14).Style.NumberFormat.Format = "#,##0.00";
            }
            sheet.Column(2).Style.DateFormat.Format = "dd/MM/yyyy HH:mm";

            AgregarHojaResumenPedidos(workbook, pedidos, from, to);

            return DescargarExcel(workbook, $"reporte-pedidos_{Sufijo(from, to)}.xlsx");
        }

        [HttpGet]
        public async Task<IActionResult> DescargarPersonalizados(DateTime? desde, DateTime? hasta)
        {
            var (from, to) = NormalizarRango(desde, hasta);

            var pedidos = await context.CustomOrderRequests
                .Include(p => p.User)
                .AsNoTracking()
                .Where(p => p.CreatedAt >= from && p.CreatedAt < to)
                .OrderBy(p => p.CreatedAt)
                .ToListAsync();

            using var workbook = new XLWorkbook();
            var sheet = workbook.Worksheets.Add("Pedidos personalizados");

            var headers = new[]
            {
                "N°", "Fecha", "Cliente", "Usuario", "Teléfono contacto",
                "Descripción", "Cantidad", "Estado", "Imagen de referencia"
            };
            EscribirEncabezados(sheet, headers);

            var fila = 2;
            foreach (var pedido in pedidos)
            {
                sheet.Cell(fila, 1).Value = pedido.Id;
                sheet.Cell(fila, 2).Value = pedido.CreatedAt.ToLocalTime();
                sheet.Cell(fila, 3).Value = pedido.User?.FullName ?? "—";
                sheet.Cell(fila, 4).Value = pedido.User?.UserName ?? "—";
                sheet.Cell(fila, 5).Value = pedido.TelefonoContacto ?? "—";
                sheet.Cell(fila, 6).Value = pedido.Descripcion;
                sheet.Cell(fila, 7).Value = pedido.Cantidad;
                sheet.Cell(fila, 8).Value = pedido.Estado;
                sheet.Cell(fila, 9).Value = pedido.ImagenReferenciaUrl ?? "—";
                fila++;
            }

            AplicarEstiloTabla(sheet, headers.Length, fila - 1);
            sheet.Column(2).Style.DateFormat.Format = "dd/MM/yyyy HH:mm";

            return DescargarExcel(workbook, $"reporte-personalizados_{Sufijo(from, to)}.xlsx");
        }

        [HttpGet]
        public async Task<IActionResult> DescargarSerigrafiado(DateTime? desde, DateTime? hasta)
        {
            var (from, to) = NormalizarRango(desde, hasta);

            var pedidos = await context.SerigrafiadoRequests
                .Include(p => p.User)
                .AsNoTracking()
                .Where(p => p.CreatedAt >= from && p.CreatedAt < to)
                .OrderBy(p => p.CreatedAt)
                .ToListAsync();

            using var workbook = new XLWorkbook();
            var sheet = workbook.Worksheets.Add("Serigrafía");

            var headers = new[]
            {
                "N°", "Fecha", "Cliente", "Usuario", "Teléfono contacto", "Prendas",
                "Tipo de tela", "Cantidad", "Descripción", "Estado", "Imagen de referencia"
            };
            EscribirEncabezados(sheet, headers);

            var fila = 2;
            foreach (var pedido in pedidos)
            {
                sheet.Cell(fila, 1).Value = pedido.Id;
                sheet.Cell(fila, 2).Value = pedido.CreatedAt.ToLocalTime();
                sheet.Cell(fila, 3).Value = pedido.User?.FullName ?? "—";
                sheet.Cell(fila, 4).Value = pedido.User?.UserName ?? "—";
                sheet.Cell(fila, 5).Value = pedido.TelefonoContacto ?? "—";
                sheet.Cell(fila, 6).Value = pedido.TiposPrenda.Replace(",", ", ");
                sheet.Cell(fila, 7).Value = pedido.TipoTela ?? "—";
                sheet.Cell(fila, 8).Value = pedido.Cantidad;
                sheet.Cell(fila, 9).Value = pedido.Descripcion;
                sheet.Cell(fila, 10).Value = pedido.Estado;
                sheet.Cell(fila, 11).Value = pedido.ImagenReferenciaUrl ?? "—";
                fila++;
            }

            AplicarEstiloTabla(sheet, headers.Length, fila - 1);
            sheet.Column(2).Style.DateFormat.Format = "dd/MM/yyyy HH:mm";

            return DescargarExcel(workbook, $"reporte-serigrafiado_{Sufijo(from, to)}.xlsx");
        }

        [HttpGet]
        public async Task<IActionResult> DescargarInventario()
        {
            var productos = await context.Products
                .AsNoTracking()
                .OrderBy(p => p.Category)
                .ThenBy(p => p.Name)
                .ToListAsync();

            using var workbook = new XLWorkbook();
            var sheet = workbook.Worksheets.Add("Inventario");

            var headers = new[]
            {
                "Nombre", "Categoría", "Género", "Tallas", "Precio (Bs)", "Stock",
                "Valor en inventario (Bs)", "Estado", "Creado"
            };
            EscribirEncabezados(sheet, headers);

            var fila = 2;
            foreach (var producto in productos)
            {
                sheet.Cell(fila, 1).Value = producto.Name;
                sheet.Cell(fila, 2).Value = producto.Category ?? "—";
                sheet.Cell(fila, 3).Value = producto.Genero ?? "—";
                sheet.Cell(fila, 4).Value = producto.Tallas ?? "—";
                sheet.Cell(fila, 5).Value = producto.Price;
                sheet.Cell(fila, 6).Value = producto.Stock;
                sheet.Cell(fila, 7).Value = producto.Price * producto.Stock;
                sheet.Cell(fila, 8).Value = producto.IsActive ? "Activo" : "Oculto";
                sheet.Cell(fila, 9).Value = producto.CreatedAt.ToLocalTime();
                fila++;
            }

            AplicarEstiloTabla(sheet, headers.Length, fila - 1);
            if (fila > 2)
            {
                sheet.Range(2, 5, fila - 1, 5).Style.NumberFormat.Format = "#,##0.00";
                sheet.Range(2, 7, fila - 1, 7).Style.NumberFormat.Format = "#,##0.00";
            }
            sheet.Column(9).Style.DateFormat.Format = "dd/MM/yyyy HH:mm";

            return DescargarExcel(workbook, $"reporte-inventario_{DateTime.Now:yyyyMMdd}.xlsx");
        }

        // ---------- Helpers ----------

        private static (DateTime from, DateTime to) NormalizarRango(DateTime? desde, DateTime? hasta)
        {
            var from = DateTime.SpecifyKind((desde ?? DateTime.UtcNow.AddMonths(-1)).Date, DateTimeKind.Utc);
            var to = DateTime.SpecifyKind((hasta ?? DateTime.UtcNow).Date.AddDays(1), DateTimeKind.Utc); // exclusivo, incluye todo el día "hasta"
            return (from, to);
        }

        private static string Sufijo(DateTime from, DateTime to) =>
            $"{from:yyyyMMdd}-{to.AddDays(-1):yyyyMMdd}";

        private static void EscribirEncabezados(IXLWorksheet sheet, string[] headers)
        {
            for (var i = 0; i < headers.Length; i++)
            {
                var cell = sheet.Cell(1, i + 1);
                cell.Value = headers[i];
            }
        }

        private static void EscribirFilaPedidoBase(IXLWorksheet sheet, int fila, Order pedido)
        {
            sheet.Cell(fila, 1).Value = pedido.Id;
            sheet.Cell(fila, 2).Value = pedido.CreatedAt.ToLocalTime();
            sheet.Cell(fila, 3).Value = pedido.User?.FullName ?? "—";
            sheet.Cell(fila, 4).Value = pedido.User?.UserName ?? "—";
            sheet.Cell(fila, 5).Value = pedido.TelefonoContacto ?? "—";
            sheet.Cell(fila, 6).Value = pedido.Status;
        }

        private static void AgregarHojaResumenPedidos(XLWorkbook workbook, List<Order> pedidos, DateTime from, DateTime to)
        {
            var resumen = workbook.Worksheets.Add("Resumen");
            resumen.Cell(1, 1).Value = "Rango del reporte";
            resumen.Cell(1, 2).Value = $"{from:dd/MM/yyyy} — {to.AddDays(-1):dd/MM/yyyy}";

            resumen.Cell(3, 1).Value = "Total de pedidos";
            resumen.Cell(3, 2).Value = pedidos.Count;

            resumen.Cell(4, 1).Value = "Monto total (Bs)";
            resumen.Cell(4, 2).Value = pedidos.Sum(p => p.Items.Sum(i => i.UnitPrice * i.Quantity));
            resumen.Cell(4, 2).Style.NumberFormat.Format = "#,##0.00";

            var porEstado = pedidos.GroupBy(p => p.Status)
                .Select(g => new { Estado = g.Key, Cantidad = g.Count() })
                .OrderByDescending(g => g.Cantidad)
                .ToList();

            resumen.Cell(6, 1).Value = "Estado";
            resumen.Cell(6, 2).Value = "Cantidad de pedidos";
            resumen.Range(6, 1, 6, 2).Style.Font.Bold = true;

            var fila = 7;
            foreach (var item in porEstado)
            {
                resumen.Cell(fila, 1).Value = item.Estado;
                resumen.Cell(fila, 2).Value = item.Cantidad;
                fila++;
            }

            resumen.Columns().AdjustToContents();
        }

        private static void AplicarEstiloTabla(IXLWorksheet sheet, int columnas, int ultimaFila)
        {
            var headerRange = sheet.Range(1, 1, 1, columnas);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#0b0b0d");
            headerRange.Style.Font.FontColor = XLColor.White;
            sheet.SheetView.FreezeRows(1);

            if (ultimaFila > 1)
            {
                var tabla = sheet.Range(1, 1, ultimaFila, columnas);
                tabla.CreateTable();
            }

            sheet.Columns().AdjustToContents();
        }

        private FileContentResult DescargarExcel(XLWorkbook workbook, string nombreArchivo)
        {
            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return File(
                stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                nombreArchivo);
        }
    }
}
