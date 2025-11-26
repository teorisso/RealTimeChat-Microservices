using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MessagesService.Entities
{
    /// <summary>
    /// Read-only entity - tabla administrada por GroupsService
    /// </summary>
    [Table("grupos")]
    public class Grupo
    {
        [Key]
        public int Id { get; set; }

        [Required, MaxLength(100)]
        public string Nombre { get; set; } = string.Empty;

        public int CreadorId { get; set; }

        public DateTime FechaCreacion { get; set; }

        public bool Activo { get; set; } = true;
    }
}
