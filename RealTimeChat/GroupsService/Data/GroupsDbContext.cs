using GroupsService.Entities;
using Microsoft.EntityFrameworkCore;

namespace GroupsService.Data
{
    public class GroupsDbContext : DbContext
    {
        public GroupsDbContext(DbContextOptions<GroupsDbContext> options) : base(options)
        {
        }

        public DbSet<Grupo> Grupos { get; set; }
        public DbSet<GrupoMiembro> GrupoMiembros { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Índices para optimización
            modelBuilder.Entity<Grupo>()
                .HasIndex(g => g.CreadorId);

            modelBuilder.Entity<GrupoMiembro>()
                .HasIndex(gm => new { gm.GrupoId, gm.UsuarioId })
                .IsUnique(); // Un usuario no puede estar duplicado en un grupo

            modelBuilder.Entity<GrupoMiembro>()
                .HasIndex(gm => gm.UsuarioId);

            // Cascade delete behavior
            modelBuilder.Entity<GrupoMiembro>()
                .HasOne(gm => gm.Grupo)
                .WithMany(g => g.Miembros)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
