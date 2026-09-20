using System.ComponentModel.DataAnnotations;

namespace EcommerceApp.Models
{
    public class OrderItem
    {
        [Key]
        public int Id { get; set; }

        public int OrderId { get; set; }
        public Order? Order { get; set; }

        public int ProductId { get; set; }
        public Product? Product { get; set; }

        // Guardamos nombre y precio "congelados" al momento de agregarlo,
        // para que si el admin cambia el precio después, el pedido ya hecho
        // no se altere.
        [Required, MaxLength(100)]
        public string ProductName { get; set; } = string.Empty;

        public decimal UnitPrice { get; set; }

        [Range(1, int.MaxValue)]
        public int Quantity { get; set; }

        public string? Talla { get; set; }
        public string? Genero { get; set; }

        // Personalización de la camiseta (opcional)
        [MaxLength(30)]
        public string? NombrePersonalizado { get; set; }

        [MaxLength(10)]
        public string? NumeroPersonalizado { get; set; }

        [MaxLength(300)]
        public string? Notas { get; set; }
    }
}
