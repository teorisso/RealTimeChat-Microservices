namespace Shared.DTOs
{
    public class TypingIndicatorDto
    {
        public string ConversacionId { get; set; } = string.Empty;
        public string UsuarioId { get; set; } = string.Empty;
        public string UsuarioNombre { get; set; } = string.Empty;
        public bool IsTyping { get; set; }
    }
}
