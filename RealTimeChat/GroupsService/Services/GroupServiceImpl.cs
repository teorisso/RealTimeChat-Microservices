using GroupsService.Data;
using GroupsService.DTOs;
using GroupsService.Entities;
using Microsoft.EntityFrameworkCore;
using Npgsql;
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
            // SOLUCIÓN ALTERNATIVA: Usar SQL directo para evitar el bug de Npgsql con Supabase
            try
            {
                var connection = _context.Database.GetDbConnection();
                var wasOpen = connection.State == System.Data.ConnectionState.Open;
                
                if (!wasOpen)
                {
                    await connection.OpenAsync();
                }

                try
                {
                    int grupoId;
                    
                    // 1. Insertar el grupo usando SQL directo
                    using (var cmd = connection.CreateCommand())
                    {
                        cmd.CommandText = @"
                            INSERT INTO grupos (""Nombre"", ""Descripcion"", ""AvatarUrl"", ""CreadorId"", ""FechaCreacion"", ""Activo"")
                            VALUES (@nombre, @descripcion, @avatarUrl, @creadorId, @fechaCreacion, @activo)
                            RETURNING ""Id"";";
                        
                        cmd.Parameters.Add(new NpgsqlParameter("@nombre", request.Nombre));
                        cmd.Parameters.Add(new NpgsqlParameter("@descripcion", (object?)request.Descripcion ?? DBNull.Value));
                        cmd.Parameters.Add(new NpgsqlParameter("@avatarUrl", (object?)request.AvatarUrl ?? DBNull.Value));
                        cmd.Parameters.Add(new NpgsqlParameter("@creadorId", creadorId));
                        cmd.Parameters.Add(new NpgsqlParameter("@fechaCreacion", DateTime.UtcNow));
                        cmd.Parameters.Add(new NpgsqlParameter("@activo", true));
                        
                        grupoId = Convert.ToInt32(await cmd.ExecuteScalarAsync());
                    }

                    // 2. Insertar el creador como miembro admin
                    using (var cmd = connection.CreateCommand())
                    {
                        cmd.CommandText = @"
                            INSERT INTO grupo_miembros (""GrupoId"", ""UsuarioId"", ""FechaUnion"", ""EsAdmin"", ""Activo"")
                            VALUES (@grupoId, @usuarioId, @fechaUnion, @esAdmin, @activo);";
                        
                        cmd.Parameters.Add(new NpgsqlParameter("@grupoId", grupoId));
                        cmd.Parameters.Add(new NpgsqlParameter("@usuarioId", creadorId));
                        cmd.Parameters.Add(new NpgsqlParameter("@fechaUnion", DateTime.UtcNow));
                        cmd.Parameters.Add(new NpgsqlParameter("@esAdmin", true));
                        cmd.Parameters.Add(new NpgsqlParameter("@activo", true));
                        
                        await cmd.ExecuteNonQueryAsync();
                    }

                    // 3. Insertar miembros iniciales
                    var todosLosMiembrosIds = new List<int> { creadorId };
                    if (request.MiembrosInicialesIds != null && request.MiembrosInicialesIds.Any())
                    {
                        foreach (var usuarioId in request.MiembrosInicialesIds.Where(id => id != creadorId))
                        {
                            using (var cmd = connection.CreateCommand())
                            {
                                cmd.CommandText = @"
                                    INSERT INTO grupo_miembros (""GrupoId"", ""UsuarioId"", ""FechaUnion"", ""EsAdmin"", ""Activo"")
                                    VALUES (@grupoId, @usuarioId, @fechaUnion, @esAdmin, @activo);";
                                
                                cmd.Parameters.Add(new NpgsqlParameter("@grupoId", grupoId));
                                cmd.Parameters.Add(new NpgsqlParameter("@usuarioId", usuarioId));
                                cmd.Parameters.Add(new NpgsqlParameter("@fechaUnion", DateTime.UtcNow));
                                cmd.Parameters.Add(new NpgsqlParameter("@esAdmin", false));
                                cmd.Parameters.Add(new NpgsqlParameter("@activo", true));
                                
                                await cmd.ExecuteNonQueryAsync();
                            }
                            
                            todosLosMiembrosIds.Add(usuarioId);
                        }
                    }

                    // 4. Crear conversación de grupo automáticamente
                    int conversacionId;
                    using (var cmd = connection.CreateCommand())
                    {
                        var fechaCreacion = DateTime.UtcNow;
                        cmd.CommandText = @"
                            INSERT INTO conversaciones (""Tipo"", ""GrupoId"", ""Usuario1Id"", ""Usuario2Id"", ""FechaCreacion"")
                            VALUES ('grupo', @grupoId, NULL, NULL, @fechaCreacion)
                            RETURNING ""Id"";";
                        
                        cmd.Parameters.Add(new NpgsqlParameter("@grupoId", grupoId));
                        cmd.Parameters.Add(new NpgsqlParameter("@fechaCreacion", fechaCreacion));
                        
                        conversacionId = Convert.ToInt32(await cmd.ExecuteScalarAsync());
                    }

                    // 5. Agregar todos los miembros como participantes de la conversación
                    foreach (var miembroId in todosLosMiembrosIds)
                    {
                        using (var cmd = connection.CreateCommand())
                        {
                            cmd.CommandText = @"
                                INSERT INTO participantes_conversacion (""ConversacionId"", ""UsuarioId"", ""FechaUnion"", ""Activo"")
                                VALUES (@conversacionId, @usuarioId, @fechaUnion, @activo);";
                            
                            cmd.Parameters.Add(new NpgsqlParameter("@conversacionId", conversacionId));
                            cmd.Parameters.Add(new NpgsqlParameter("@usuarioId", miembroId));
                            cmd.Parameters.Add(new NpgsqlParameter("@fechaUnion", DateTime.UtcNow));
                            cmd.Parameters.Add(new NpgsqlParameter("@activo", true));
                            
                            await cmd.ExecuteNonQueryAsync();
                        }
                    }

                    _logger.LogInformation("Grupo {GrupoId} creado con conversación {ConversacionId} y {NumMiembros} miembros", 
                        grupoId, conversacionId, todosLosMiembrosIds.Count);

                    // Retornar DTO simplificado sin consultar nuevamente la BD
                    return new GrupoDto
                    {
                        Id = grupoId.ToString(),
                        Nombre = request.Nombre,
                        Descripcion = request.Descripcion,
                        AvatarUrl = request.AvatarUrl,
                        CreadorId = creadorId.ToString(),
                        FechaCreacion = DateTime.UtcNow,
                        CantidadMiembros = 1 + (request.MiembrosInicialesIds?.Count(id => id != creadorId) ?? 0),
                        Miembros = new List<UsuarioDto>() // Lista vacía por performance
                    };
                }
                finally
                {
                    if (!wasOpen && connection.State == System.Data.ConnectionState.Open)
                    {
                        await connection.CloseAsync();
                    }
                }
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
            // Optimizado: Una sola query con JOIN en lugar de dos queries separadas
            var grupos = await _context.Grupos
                .Include(g => g.Miembros.Where(m => m.Activo))
                .Where(g => g.Activo && g.Miembros.Any(m => m.UsuarioId == userId && m.Activo))
                .AsSplitQuery() // Mejor performance para Include
                .ToListAsync();

            return grupos.Select(g => new GrupoDto
            {
                Id = g.Id.ToString(),
                Nombre = g.Nombre,
                Descripcion = g.Descripcion,
                AvatarUrl = g.AvatarUrl,
                CreadorId = g.CreadorId.ToString(),
                FechaCreacion = g.FechaCreacion,
                CantidadMiembros = g.Miembros.Count,
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
