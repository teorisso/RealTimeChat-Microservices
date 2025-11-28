using System.ComponentModel.DataAnnotations;

namespace MessagesService.DTOs
{
    public class SendMessageRequest
    {
        public int ConversacionId { get; set; } // Puede ser 0 para conversaciones draft

        [Required, MaxLength(5000)]
        public string Contenido { get; set; } = string.Empty;

        public int? DestinatarioId { get; set; } // Para conversaciones directas que aún no existen
    }
}
