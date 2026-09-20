using System.ComponentModel.DataAnnotations;

namespace EcommerceApp.Models
{
    // Un pedido personalizado "desde cero": el cliente describe lo que
    // quiere, sube una imagen de referencia, y el administrador lo revisa
    // y lo contacta directamente (no pasa por el carrito normal).
    public class CustomOrderRequest
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;
        public ApplicationUser? User { get; set; }

        [Required(ErrorMessage = "Cuéntanos qué quieres pedir")]
        [MaxLength(2000)]
        public string Descripcion { get; set; } = string.Empty;

        [Range(1, 10000, ErrorMessage = "La cantidad debe ser mayor a 0")]
        public int Cantidad { get; set; } = 1;

        public string? ImagenReferenciaUrl { get; set; }

        [MaxLength(30)]
        public string? TelefonoContacto { get; set; }

        // Pendiente -> Contactado -> Completado
        [MaxLength(20)]
        public string Estado { get; set; } = "Pendiente";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
