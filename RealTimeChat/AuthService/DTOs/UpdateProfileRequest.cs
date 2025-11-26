using System.ComponentModel.DataAnnotations;

namespace AuthService.DTOs
{
    public class UpdateProfileRequest
    {
        [MaxLength(100, ErrorMessage = "El nombre no puede exceder 100 caracteres")]
        public string? Nombre { get; set; }

        [MaxLength(500, ErrorMessage = "La URL del avatar no puede exceder 500 caracteres")]
        [Url(ErrorMessage = "La URL del avatar no tiene un formato válido")]
        public string? AvatarUrl { get; set; }
    }
}
