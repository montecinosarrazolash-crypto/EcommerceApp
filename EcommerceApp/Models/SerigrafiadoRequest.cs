using System.ComponentModel.DataAnnotations;

namespace EcommerceApp.Models
{
    // Pedido de cotización de serigrafía/estampado: el cliente elige qué tipo
    // de prendas quiere, en qué tela, cuántas, y describe el diseño. El
    // administrador lo revisa y contacta al cliente con la tela y el costo.
    public class SerigrafiadoRequest
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;
        public ApplicationUser? User { get; set; }

        // Guardado como texto separado por comas, ej: "Poleras,Buzos,Gorras"
        [Required(ErrorMessage = "Elige al menos un tipo de prenda")]
        [MaxLength(300)]
        public string TiposPrenda { get; set; } = string.Empty;

        [MaxLength(50)]
        public string? TipoTela { get; set; }

        [Range(1, 100000, ErrorMessage = "La cantidad debe ser mayor a 0")]
        public int Cantidad { get; set; } = 1;

        [Required(ErrorMessage = "Cuéntanos cómo quieres el estampado")]
        [MaxLength(2000)]
        public string Descripcion { get; set; } = string.Empty;

        public string? ImagenReferenciaUrl { get; set; }

        [MaxLength(30)]
        public string? TelefonoContacto { get; set; }

        // Pendiente -> Cotizado -> Confirmado -> Completado
        [MaxLength(20)]
        public string Estado { get; set; } = "Pendiente";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
