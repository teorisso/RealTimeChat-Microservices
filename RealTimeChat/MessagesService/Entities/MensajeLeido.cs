using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MessagesService.Entities
{
    [Table("mensajes_leidos")]
    public class MensajeLeido
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int MensajeId { get; set; }

        [Required]
        public int UsuarioId { get; set; } // FK to AuthService.Usuario

        public DateTime FechaLectura { get; set; } = DateTime.UtcNow;

        [ForeignKey("MensajeId")]
        public Mensaje Mensaje { get; set; } = null!;
    }
}
