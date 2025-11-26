namespace Shared.DTOs
{
    public class GrupoDto
    {
        public string Id { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public string? Descripcion { get; set; }
        public string? AvatarUrl { get; set; }
        public string CreadorId { get; set; } = string.Empty;
        public DateTime FechaCreacion { get; set; }
        public int CantidadMiembros { get; set; }
        public List<UsuarioDto> Miembros { get; set; } = new();
    }
}
