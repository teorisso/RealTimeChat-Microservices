namespace Shared.DTOs
{
    public class MessageDto
    {
        public string Id { get; set; } = string.Empty;
        public string ConversacionId { get; set; } = string.Empty;
        public string RemitenteId { get; set; } = string.Empty;
        public string RemitenteNombre { get; set; } = string.Empty;
        public string Contenido { get; set; } = string.Empty;
        public DateTime FechaEnvio { get; set; }
        public bool Eliminado { get; set; }
        public int CantidadLecturas { get; set; }
        public bool LeidoPorMi { get; set; }
    }
}
