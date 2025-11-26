using System.ComponentModel.DataAnnotations;

namespace MessagesService.DTOs
{
    public class GetMessagesRequest
    {
        [Required]
        public int ConversacionId { get; set; }

        public int Page { get; set; } = 1;

        public int PageSize { get; set; } = 50;
    }
}
