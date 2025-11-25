using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AuthService.Entities
{
    // "Table" define el nombre real que tendrá en Supabase
    [Table("usuarios")]
    public class Usuario
    {
        [Key]
        public int Id { get; set; } // O puedes usar Guid si prefieres IDs largos

        [Required]
        [MaxLength(100)]
        public string Nombre { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string PasswordHash { get; set; } = string.Empty;

        // Opcional: Fecha de registro para auditoría
        public DateTime FechaRegistro { get; set; } = DateTime.UtcNow;
    }
}