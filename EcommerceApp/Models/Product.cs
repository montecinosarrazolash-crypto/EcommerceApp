using System.ComponentModel.DataAnnotations;

namespace EcommerceApp.Models
{
    public class Product
    {
        [Key]
        public int Id { get; set; }

        [Required, MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required, MaxLength(500)]
        public string Description { get; set; } = string.Empty;

        [Required, Range(0.01, 999999.99)]
        public decimal Price { get; set; }

        [Required, Range(0, int.MaxValue)]
        public int Stock { get; set; }

        public string? ImageUrl { get; set; }
        public string? ImageUrlFrente { get; set; }
        public string? ImageUrlEspalda { get; set; }
        public string? Category { get; set; }

        public string? Genero { get; set; } // "Masculino", "Femenino" o "Unisex"
        public string? Tallas { get; set; } // ej: "S,M,L,XL"

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        // Si un producto ya tiene pedidos asociados, no se puede borrar sin
        // romper el historial de ventas — en ese caso lo "ocultamos" en vez
        // de eliminarlo de verdad, apagando este campo.
        public bool IsActive { get; set; } = true;
    }
}
