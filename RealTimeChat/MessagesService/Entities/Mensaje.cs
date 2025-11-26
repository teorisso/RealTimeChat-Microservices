using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MessagesService.Entities
{
    [Table("mensajes")]
    public class Mensaje
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ConversacionId { get; set; }

        [Required]
        public int RemitenteId { get; set; } // FK to AuthService.Usuario

        [Required, MaxLength(5000)]
        public string Contenido { get; set; } = string.Empty;

        public DateTime FechaEnvio { get; set; } = DateTime.UtcNow;

        public bool Eliminado { get; set; } = false; // Soft delete

        [ForeignKey("ConversacionId")]
        public Conversacion Conversacion { get; set; } = null!;

        public ICollection<MensajeLeido> Lecturas { get; set; } = new List<MensajeLeido>();
    }
}
