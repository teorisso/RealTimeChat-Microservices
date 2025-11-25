namespace Shared.DTOs
{
    public class UsuarioDto
    {
        public string Id { get; set; } = string.Empty; // Usaremos string para Guids o IDs de Identity
        public string Nombre { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        // No ponemos password aquí por seguridad
    }
}