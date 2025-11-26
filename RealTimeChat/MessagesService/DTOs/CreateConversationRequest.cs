using System.ComponentModel.DataAnnotations;

namespace MessagesService.DTOs
{
    public class CreateConversationRequest
    {
        [Required]
        public string Tipo { get; set; } = "directa";

        public int? OtroUsuarioId { get; set; } // For direct chats

        public int? GrupoId { get; set; } // For group chats

        public List<int>? ParticipantesIds { get; set; }
    }
}
