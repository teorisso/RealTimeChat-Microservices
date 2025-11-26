namespace Shared.DTOs
{
    public class ReadReceiptDto
    {
        public string Id { get; set; } = string.Empty;
        public string MensajeId { get; set; } = string.Empty;
        public string UsuarioId { get; set; } = string.Empty;
        public string UsuarioNombre { get; set; } = string.Empty;
        public DateTime FechaLectura { get; set; }
    }
}
