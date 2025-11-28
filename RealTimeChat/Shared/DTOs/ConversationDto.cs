namespace Shared.DTOs
{
    public class ConversationDto
    {
        public string Id { get; set; } = string.Empty;
        public string Tipo { get; set; } = string.Empty;
        public int? Usuario1Id { get; set; }
        public int? Usuario2Id { get; set; }
        public int? GrupoId { get; set; }
        public string? GrupoNombre { get; set; } // Nombre del grupo
        public DateTime FechaCreacion { get; set; }
        public MessageDto? UltimoMensaje { get; set; }
        public int MensajesNoLeidos { get; set; }
        public List<int> ParticipantesIds { get; set; } = new();
    }
}
