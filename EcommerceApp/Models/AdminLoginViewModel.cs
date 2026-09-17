using System.ComponentModel.DataAnnotations;

namespace EcommerceApp.Models
{
    public class AdminLoginViewModel
    {
        [Required(ErrorMessage = "El usuario es obligatorio")]
        [Display(Name = "Usuario administrador")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "El código de administrador es obligatorio")]
        [DataType(DataType.Password)]
        [Display(Name = "Código de administrador")]
        public string AdminCode { get; set; } = string.Empty;
    }
}
