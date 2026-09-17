using System.ComponentModel.DataAnnotations;

namespace EcommerceApp.Models
{
    public class Order
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        // "Carrito" = todavía lo está armando el cliente (no es un pedido real todavía).
        // "Pendiente" = el cliente confirmó el pedido.
        // "Completado" / "Cancelado" = lo gestiona el administrador.
        [Required, MaxLength(20)]
        public string Status { get; set; } = "Carrito";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        public List<OrderItem> Items { get; set; } = new();
    }
}
