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
    // un rango de fechas opcional (según la fecha de creación de cada registro)
    // y llevan un título con el nombre del reporte y la fecha en que se generó.
    [Authorize(Roles = "Admin")]
    public class ReportsController(ApplicationDbContext context) : Controller
    {
        // Color de marca (mismo rojo del panel de administrador).
        private static readonly XLColor MarcaRojo = XLColor.FromHtml("#c81e2c");
        private static readonly XLColor MarcaNegro = XLColor.FromHtml("#141414");

        // Filas fijas que ocupa el bloque de título en cada hoja:
        // 1) Título del reporte  2) Subtítulo (rango / fecha)  3) en blanco  4) encabezados  5) datos...
        private const int FilaTitulo = 1;
        private const int FilaSubtitulo = 2;
        private const int FilaEncabezados = 4;
        private const int FilaDatosInicio = 5;

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

            EscribirTitulo(sheet, "REPORTE DE PEDIDOS", RangoTexto(from, to), headers.Length);
            EscribirEncabezados(sheet, headers);

            var fila = FilaDatosInicio;
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
            if (fila > FilaDatosInicio)
            {
                sheet.Range(FilaDatosInicio, 13, fila - 1, 14).Style.NumberFormat.Format = "#,##0.00";
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

            EscribirTitulo(sheet, "REPORTE DE PEDIDOS PERSONALIZADOS", RangoTexto(from, to), headers.Length);
            EscribirEncabezados(sheet, headers);

            var fila = FilaDatosInicio;
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

            EscribirTitulo(sheet, "REPORTE DE SERIGRAFÍA", RangoTexto(from, to), headers.Length);
            EscribirEncabezados(sheet, headers);

            var fila = FilaDatosInicio;
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

            EscribirTitulo(sheet, "REPORTE DE INVENTARIO", $"Generado el {DateTime.Now:dd/MM/yyyy HH:mm}", headers.Length);
            EscribirEncabezados(sheet, headers);

            var fila = FilaDatosInicio;
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
            if (fila > FilaDatosInicio)
            {
                sheet.Range(FilaDatosInicio, 5, fila - 1, 5).Style.NumberFormat.Format = "#,##0.00";
                sheet.Range(FilaDatosInicio, 7, fila - 1, 7).Style.NumberFormat.Format = "#,##0.00";
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

        private static string RangoTexto(DateTime from, DateTime to) =>
            $"Del {from:dd/MM/yyyy} al {to.AddDays(-1):dd/MM/yyyy}  ·  Generado el {DateTime.Now:dd/MM/yyyy HH:mm}";

        // Escribe el bloque de título (nombre del reporte + rango/fecha de
        // generación) en las filas 1 y 2 de la hoja, fusionado a lo ancho de
        // las columnas de la tabla.
        private static void EscribirTitulo(IXLWorksheet sheet, string titulo, string subtitulo, int columnas)
        {
            var filaTitulo = sheet.Range(FilaTitulo, 1, FilaTitulo, columnas);
            filaTitulo.Merge();
            var celdaTitulo = sheet.Cell(FilaTitulo, 1);
            celdaTitulo.Value = titulo;
            celdaTitulo.Style.Font.Bold = true;
            celdaTitulo.Style.Font.FontSize = 15;
            celdaTitulo.Style.Font.FontColor = XLColor.White;
            celdaTitulo.Style.Fill.BackgroundColor = MarcaRojo;
            celdaTitulo.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            celdaTitulo.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            sheet.Row(FilaTitulo).Height = 26;

            var filaSub = sheet.Range(FilaSubtitulo, 1, FilaSubtitulo, columnas);
            filaSub.Merge();
            var celdaSub = sheet.Cell(FilaSubtitulo, 1);
            celdaSub.Value = subtitulo;
            celdaSub.Style.Font.Italic = true;
            celdaSub.Style.Font.FontSize = 10;
            celdaSub.Style.Font.FontColor = XLColor.FromHtml("#595959");
            celdaSub.Style.Fill.BackgroundColor = XLColor.FromHtml("#f2f2f2");
            celdaSub.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            celdaSub.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            sheet.Row(FilaSubtitulo).Height = 20;

            // Fila 3 se deja en blanco como separador visual antes de la tabla.
        }

        private static void EscribirEncabezados(IXLWorksheet sheet, string[] headers)
        {
            for (var i = 0; i < headers.Length; i++)
            {
                sheet.Cell(FilaEncabezados, i + 1).Value = headers[i];
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

            EscribirTitulo(resumen, "RESUMEN DE PEDIDOS", RangoTexto(from, to), 2);

            var fila = FilaEncabezados;
            resumen.Cell(fila, 1).Value = "Total de pedidos";
            resumen.Cell(fila, 2).Value = pedidos.Count;
            fila++;

            resumen.Cell(fila, 1).Value = "Monto total (Bs)";
            resumen.Cell(fila, 2).Value = pedidos.Sum(p => p.Items.Sum(i => i.UnitPrice * i.Quantity));
            resumen.Cell(fila, 2).Style.NumberFormat.Format = "#,##0.00";
            fila += 2;

            var porEstado = pedidos.GroupBy(p => p.Status)
                .Select(g => new { Estado = g.Key, Cantidad = g.Count() })
                .OrderByDescending(g => g.Cantidad)
                .ToList();

            var filaHeaderEstado = fila;
            resumen.Cell(fila, 1).Value = "Estado";
            resumen.Cell(fila, 2).Value = "Cantidad de pedidos";
            fila++;

            var filaInicioEstados = fila;
            foreach (var item in porEstado)
            {
                resumen.Cell(fila, 1).Value = item.Estado;
                resumen.Cell(fila, 2).Value = item.Cantidad;
                fila++;
            }

            var headerEstadoRange = resumen.Range(filaHeaderEstado, 1, filaHeaderEstado, 2);
            headerEstadoRange.Style.Font.Bold = true;
            headerEstadoRange.Style.Fill.BackgroundColor = MarcaNegro;
            headerEstadoRange.Style.Font.FontColor = XLColor.White;

            if (fila - 1 >= filaInicioEstados)
            {
                var cuerpoEstados = resumen.Range(filaInicioEstados, 1, fila - 1, 2);
                cuerpoEstados.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                cuerpoEstados.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
            }

            resumen.Columns().AdjustToContents();
        }

        private static void AplicarEstiloTabla(IXLWorksheet sheet, int columnas, int ultimaFila)
        {
            var headerRange = sheet.Range(FilaEncabezados, 1, FilaEncabezados, columnas);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = MarcaNegro;
            headerRange.Style.Font.FontColor = XLColor.White;
            headerRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            sheet.Row(FilaEncabezados).Height = 20;
            sheet.SheetView.FreezeRows(FilaEncabezados);

            if (ultimaFila >= FilaDatosInicio)
            {
                var tabla = sheet.Range(FilaEncabezados, 1, ultimaFila, columnas);
                var tablaExcel = tabla.CreateTable();
                tablaExcel.Theme = XLTableTheme.TableStyleMedium2;

                sheet.Range(FilaEncabezados, 1, ultimaFila, columnas).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            }

            sheet.Columns().AdjustToContents();
            // El título fusionado no debe forzar a que la primera columna quede angosta.
            sheet.Column(1).Width = Math.Max(sheet.Column(1).Width, 12);
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
