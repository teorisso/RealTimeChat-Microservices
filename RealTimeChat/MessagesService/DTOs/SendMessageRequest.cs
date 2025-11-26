using System.ComponentModel.DataAnnotations;

namespace MessagesService.DTOs
{
    public class SendMessageRequest
    {
        [Required]
        public int ConversacionId { get; set; }

        [Required, MaxLength(5000)]
        public string Contenido { get; set; } = string.Empty;
    }
}
