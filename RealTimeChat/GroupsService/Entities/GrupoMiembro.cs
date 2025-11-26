using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GroupsService.Entities
{
    [Table("grupo_miembros")]
    public class GrupoMiembro
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int GrupoId { get; set; }

        [Required]
        public int UsuarioId { get; set; }

        public DateTime FechaUnion { get; set; } = DateTime.UtcNow;

        public bool EsAdmin { get; set; } = false;

        public bool Activo { get; set; } = true;

        // Relación con grupo
        [ForeignKey("GrupoId")]
        public Grupo Grupo { get; set; } = null!;
    }
}
