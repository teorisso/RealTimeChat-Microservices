using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MessagesService.Entities
{
    [Table("participantes_conversacion")]
    public class ParticipanteConversacion
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ConversacionId { get; set; }

        [Required]
        public int UsuarioId { get; set; }

        public DateTime FechaUnion { get; set; } = DateTime.UtcNow;

        public bool Activo { get; set; } = true;

        [ForeignKey("ConversacionId")]
        public Conversacion Conversacion { get; set; } = null!;
    }
}
