using System.ComponentModel.DataAnnotations;

namespace GroupsService.DTOs
{
    public class CreateGrupoRequest
    {
        [Required(ErrorMessage = "El nombre del grupo es obligatorio")]
        [MaxLength(100)]
        public string Nombre { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Descripcion { get; set; }

        [Url(ErrorMessage = "La URL del avatar no es válida")]
        [MaxLength(500)]
        public string? AvatarUrl { get; set; }

        public List<int>? MiembrosInicialesIds { get; set; }
    }
}
