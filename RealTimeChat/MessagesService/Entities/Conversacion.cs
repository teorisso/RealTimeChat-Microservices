using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MessagesService.Entities
{
    [Table("conversaciones")]
    public class Conversacion
    {
        [Key]
        public int Id { get; set; }

        [MaxLength(50)]
        public string Tipo { get; set; } = "directa"; // "directa" o "grupo"

        // For 1:1 conversations
        public int? Usuario1Id { get; set; }
        public int? Usuario2Id { get; set; }

        // For group conversations
        public int? GrupoId { get; set; }

        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

        public ICollection<Mensaje> Mensajes { get; set; } = new List<Mensaje>();
        public ICollection<ParticipanteConversacion> Participantes { get; set; } = new List<ParticipanteConversacion>();
    }
}
