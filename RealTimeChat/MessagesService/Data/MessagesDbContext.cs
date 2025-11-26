using MessagesService.Entities;
using Microsoft.EntityFrameworkCore;

namespace MessagesService.Data
{
    public class MessagesDbContext : DbContext
    {
        public MessagesDbContext(DbContextOptions<MessagesDbContext> options) : base(options) { }

        // MessagesService tables
        public DbSet<Conversacion> Conversaciones { get; set; }
        public DbSet<Mensaje> Mensajes { get; set; }
        public DbSet<MensajeLeido> MensajesLeidos { get; set; }
        public DbSet<ParticipanteConversacion> ParticipantesConversacion { get; set; }

        // Read-only tables (managed by GroupsService)
        public DbSet<Grupo> Grupos { get; set; }
        public DbSet<GrupoMiembro> GrupoMiembros { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Performance indexes
            modelBuilder.Entity<Mensaje>()
                .HasIndex(m => m.ConversacionId);

            modelBuilder.Entity<Mensaje>()
                .HasIndex(m => m.RemitenteId);

            modelBuilder.Entity<Mensaje>()
                .HasIndex(m => m.FechaEnvio);

            // Unique composite indexes
            modelBuilder.Entity<MensajeLeido>()
                .HasIndex(ml => new { ml.MensajeId, ml.UsuarioId })
                .IsUnique(); // One read receipt per user per message

            modelBuilder.Entity<ParticipanteConversacion>()
                .HasIndex(pc => new { pc.ConversacionId, pc.UsuarioId })
                .IsUnique(); // No duplicate participants

            // Cascade delete behavior
            modelBuilder.Entity<Mensaje>()
                .HasOne(m => m.Conversacion)
                .WithMany(c => c.Mensajes)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<MensajeLeido>()
                .HasOne(ml => ml.Mensaje)
                .WithMany(m => m.Lecturas)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
