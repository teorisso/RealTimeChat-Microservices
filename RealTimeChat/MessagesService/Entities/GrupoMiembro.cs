using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MessagesService.Entities
{
    /// <summary>
    /// Read-only entity - tabla administrada por GroupsService
    /// </summary>
    [Table("grupo_miembros")]
    public class GrupoMiembro
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int GrupoId { get; set; }

        [Required]
        public int UsuarioId { get; set; }

        public DateTime FechaUnion { get; set; }

        public bool EsAdmin { get; set; } = false;

        public bool Activo { get; set; } = true;
    }
}
