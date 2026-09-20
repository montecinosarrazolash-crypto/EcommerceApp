using System.ComponentModel.DataAnnotations;

namespace EcommerceApp.Models
{
    public class Order
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;
        public ApplicationUser? User { get; set; }

        // "Carrito" = todavía lo está armando el cliente (no es un pedido real todavía).
        // "Pendiente" = el cliente confirmó el pedido.
        // "Completado" / "Cancelado" = lo gestiona el administrador.
        [Required, MaxLength(20)]
        public string Status { get; set; } = "Carrito";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        // Para que el administrador pueda contactar al cliente cuando el
        // pedido esté listo (WhatsApp). Se pide al confirmar el carrito.
        [MaxLength(30)]
        public string? TelefonoContacto { get; set; }

        public List<OrderItem> Items { get; set; } = new();
    }
}
