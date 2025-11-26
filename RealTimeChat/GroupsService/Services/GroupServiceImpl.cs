using GroupsService.Data;
using GroupsService.DTOs;
using GroupsService.Entities;
using Microsoft.EntityFrameworkCore;
using Shared.DTOs;

namespace GroupsService.Services
{
    public class GroupServiceImpl : IGroupService
    {
        private readonly GroupsDbContext _context;
        private readonly ILogger<GroupServiceImpl> _logger;

        public GroupServiceImpl(GroupsDbContext context, ILogger<GroupServiceImpl> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<GrupoDto?> CreateGroupAsync(int creadorId, CreateGrupoRequest request)
        {
            try
            {
                // Crear el grupo
                var grupo = new Grupo
                {
                    Nombre = request.Nombre,
                    Descripcion = request.Descripcion,
                    AvatarUrl = request.AvatarUrl,
                    CreadorId = creadorId,
                    FechaCreacion = DateTime.UtcNow,
                    Activo = true
                };

                _context.Grupos.Add(grupo);
                await _context.SaveChangesAsync();

                // Agregar el creador como miembro admin
                var creadorMiembro = new GrupoMiembro
                {
                    GrupoId = grupo.Id,
                    UsuarioId = creadorId,
                    FechaUnion = DateTime.UtcNow,
                    EsAdmin = true,
                    Activo = true
                };

                _context.GrupoMiembros.Add(creadorMiembro);

                // Agregar miembros iniciales si se proporcionaron
                if (request.MiembrosInicialesIds != null && request.MiembrosInicialesIds.Any())
                {
                    foreach (var usuarioId in request.MiembrosInicialesIds.Where(id => id != creadorId))
                    {
                        var miembro = new GrupoMiembro
                        {
                            GrupoId = grupo.Id,
                            UsuarioId = usuarioId,
                            FechaUnion = DateTime.UtcNow,
                            EsAdmin = false,
                            Activo = true
                        };

                        _context.GrupoMiembros.Add(miembro);
                    }
                }

                await _context.SaveChangesAsync();

                return await GetGroupAsync(grupo.Id, creadorId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear grupo");
                return null;
            }
        }

        public async Task<GrupoDto?> GetGroupAsync(int grupoId, int userId)
        {
            // Verificar que el usuario sea miembro del grupo
            if (!await IsUserMemberAsync(grupoId, userId))
            {
                return null;
            }

            var grupo = await _context.Grupos
                .Include(g => g.Miembros)
                .FirstOrDefaultAsync(g => g.Id == grupoId && g.Activo);

            if (grupo == null)
            {
                return null;
            }

            // Para el DTO completo, normalmente haríamos una llamada a AuthService
            // para obtener los datos de los usuarios. Por simplicidad, creamos
            // DTOs básicos con solo los IDs
            var miembrosDto = grupo.Miembros
                .Where(m => m.Activo)
                .Select(m => new UsuarioDto
                {
                    Id = m.UsuarioId.ToString(),
                    Nombre = $"Usuario {m.UsuarioId}", // Placeholder
                    Email = $"user{m.UsuarioId}@example.com" // Placeholder
                }).ToList();

            return new GrupoDto
            {
                Id = grupo.Id.ToString(),
                Nombre = grupo.Nombre,
                Descripcion = grupo.Descripcion,
                AvatarUrl = grupo.AvatarUrl,
                CreadorId = grupo.CreadorId.ToString(),
                FechaCreacion = grupo.FechaCreacion,
                CantidadMiembros = grupo.Miembros.Count(m => m.Activo),
                Miembros = miembrosDto
            };
        }

        public async Task<List<GrupoDto>> GetUserGroupsAsync(int userId)
        {
            var grupoIds = await _context.GrupoMiembros
                .Where(gm => gm.UsuarioId == userId && gm.Activo)
                .Select(gm => gm.GrupoId)
                .ToListAsync();

            var grupos = await _context.Grupos
                .Include(g => g.Miembros)
                .Where(g => grupoIds.Contains(g.Id) && g.Activo)
                .ToListAsync();

            return grupos.Select(g => new GrupoDto
            {
                Id = g.Id.ToString(),
                Nombre = g.Nombre,
                Descripcion = g.Descripcion,
                AvatarUrl = g.AvatarUrl,
                CreadorId = g.CreadorId.ToString(),
                FechaCreacion = g.FechaCreacion,
                CantidadMiembros = g.Miembros.Count(m => m.Activo),
                Miembros = new List<UsuarioDto>() // Lista vacía para performance
            }).ToList();
        }

        public async Task<bool> UpdateGroupAsync(int grupoId, int userId, UpdateGrupoRequest request)
        {
            // Solo los admins pueden actualizar el grupo
            if (!await IsUserAdminAsync(grupoId, userId))
            {
                return false;
            }

            var grupo = await _context.Grupos.FindAsync(grupoId);
            if (grupo == null || !grupo.Activo)
            {
                return false;
            }

            // Actualizar solo los campos proporcionados
            if (!string.IsNullOrWhiteSpace(request.Nombre))
            {
                grupo.Nombre = request.Nombre;
            }

            if (request.Descripcion != null)
            {
                grupo.Descripcion = request.Descripcion;
            }

            if (request.AvatarUrl != null)
            {
                grupo.AvatarUrl = request.AvatarUrl;
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteGroupAsync(int grupoId, int userId)
        {
            var grupo = await _context.Grupos.FindAsync(grupoId);
            if (grupo == null)
            {
                return false;
            }

            // Solo el creador puede eliminar el grupo
            if (grupo.CreadorId != userId)
            {
                return false;
            }

            // Soft delete
            grupo.Activo = false;
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> AddMemberAsync(int grupoId, int userId, AddMemberRequest request)
        {
            // Solo los admins pueden agregar miembros
            if (!await IsUserAdminAsync(grupoId, userId))
            {
                return false;
            }

            // Verificar que el grupo existe y está activo
            var grupo = await _context.Grupos.FindAsync(grupoId);
            if (grupo == null || !grupo.Activo)
            {
                return false;
            }

            // Verificar si el usuario ya es miembro
            var existingMember = await _context.GrupoMiembros
                .FirstOrDefaultAsync(gm => gm.GrupoId == grupoId && gm.UsuarioId == request.UsuarioId);

            if (existingMember != null)
            {
                // Si ya era miembro pero estaba inactivo, reactivarlo
                if (!existingMember.Activo)
                {
                    existingMember.Activo = true;
                    existingMember.EsAdmin = request.EsAdmin;
                    existingMember.FechaUnion = DateTime.UtcNow;
                    await _context.SaveChangesAsync();
                    return true;
                }

                // Ya es miembro activo
                return false;
            }

            // Agregar nuevo miembro
            var nuevoMiembro = new GrupoMiembro
            {
                GrupoId = grupoId,
                UsuarioId = request.UsuarioId,
                FechaUnion = DateTime.UtcNow,
                EsAdmin = request.EsAdmin,
                Activo = true
            };

            _context.GrupoMiembros.Add(nuevoMiembro);
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> RemoveMemberAsync(int grupoId, int userId, int miembroId)
        {
            // Solo los admins pueden remover miembros
            if (!await IsUserAdminAsync(grupoId, userId))
            {
                return false;
            }

            // No permitir remover al creador del grupo
            var grupo = await _context.Grupos.FindAsync(grupoId);
            if (grupo == null || grupo.CreadorId == miembroId)
            {
                return false;
            }

            var miembro = await _context.GrupoMiembros
                .FirstOrDefaultAsync(gm => gm.GrupoId == grupoId && gm.UsuarioId == miembroId);

            if (miembro == null)
            {
                return false;
            }

            // Soft delete del miembro
            miembro.Activo = false;
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> IsUserMemberAsync(int grupoId, int userId)
        {
            return await _context.GrupoMiembros
                .AnyAsync(gm => gm.GrupoId == grupoId && gm.UsuarioId == userId && gm.Activo);
        }

        public async Task<bool> IsUserAdminAsync(int grupoId, int userId)
        {
            return await _context.GrupoMiembros
                .AnyAsync(gm => gm.GrupoId == grupoId && gm.UsuarioId == userId && gm.EsAdmin && gm.Activo);
        }
    }
}
