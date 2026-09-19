using System.ComponentModel.DataAnnotations;

namespace EcommerceApp.Models
{
    public class RegisterViewModel
    {
        [Required(ErrorMessage = "El usuario es obligatorio")]
        [StringLength(20, MinimumLength = 3, ErrorMessage = "El usuario debe tener entre 3 y 20 caracteres")]
        [RegularExpression(@"^\S+$", ErrorMessage = "El usuario no puede tener espacios")]
        [Display(Name = "Usuario")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "El correo es obligatorio")]
        [EmailAddress(ErrorMessage = "Ingresa un correo válido")]
        [Display(Name = "Correo electrónico")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "La contraseña es obligatoria")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "La contraseña debe tener al menos 6 caracteres")]
        [RegularExpression(@"^(?=.*[A-Za-z])(?=.*\d).+$", ErrorMessage = "La contraseña debe incluir al menos una letra y un número")]
        [DataType(DataType.Password)]
        [Display(Name = "Contraseña")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Confirma tu contraseña")]
        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "Las contraseñas no coinciden")]
        [Display(Name = "Confirmar contraseña")]
        public string ConfirmPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "El nombre completo es obligatorio")]
        [StringLength(100, ErrorMessage = "Máximo 100 caracteres")]
        [Display(Name = "Nombre completo")]
        public string FullName { get; set; } = string.Empty;

        [StringLength(200, ErrorMessage = "Máximo 200 caracteres")]
        [Display(Name = "Dirección")]
        public string? Address { get; set; }
    }
}
