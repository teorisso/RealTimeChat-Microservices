using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GroupsService.Entities
{
    [Table("grupos")]
    public class Grupo
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Nombre { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Descripcion { get; set; }

        [MaxLength(500)]
        public string? AvatarUrl { get; set; }

        [Required]
        public int CreadorId { get; set; } // FK to Usuario

        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

        public bool Activo { get; set; } = true;

        // Relación con miembros
        public ICollection<GrupoMiembro> Miembros { get; set; } = new List<GrupoMiembro>();
    }
}
