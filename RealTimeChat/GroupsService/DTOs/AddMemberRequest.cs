using System.ComponentModel.DataAnnotations;

namespace GroupsService.DTOs
{
    public class AddMemberRequest
    {
        [Required(ErrorMessage = "El ID del usuario es obligatorio")]
        public int UsuarioId { get; set; }

        public bool EsAdmin { get; set; } = false;
    }
}
