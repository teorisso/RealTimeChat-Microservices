using System.ComponentModel.DataAnnotations;

namespace GroupsService.DTOs
{
    public class UpdateGrupoRequest
    {
        [MaxLength(100)]
        public string? Nombre { get; set; }

        [MaxLength(500)]
        public string? Descripcion { get; set; }

        [Url(ErrorMessage = "La URL del avatar no es válida")]
        [MaxLength(500)]
        public string? AvatarUrl { get; set; }
    }
}
