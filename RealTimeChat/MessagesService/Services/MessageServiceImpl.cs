using MessagesService.Data;
using MessagesService.DTOs;
using MessagesService.Entities;
using Microsoft.EntityFrameworkCore;
using Shared.DTOs;

namespace MessagesService.Services
{
    public class MessageServiceImpl : IMessageService
    {
        private readonly MessagesDbContext _context;
        private readonly ILogger<MessageServiceImpl> _logger;
        private readonly IUserInfoService _userInfoService;

        public MessageServiceImpl(
            MessagesDbContext context,
            ILogger<MessageServiceImpl> logger,
            IUserInfoService userInfoService)
        {
            _context = context;
            _logger = logger;
            _userInfoService = userInfoService;
        }

        // ===== CONVERSACIONES =====

        public async Task<ConversationDto?> CreateConversationAsync(int userId, CreateConversationRequest request)
        {
            try
            {
                var conversacion = new Conversacion
                {
                    Tipo = request.Tipo,
                    FechaCreacion = DateTime.UtcNow
                };

                if (request.Tipo == "directa")
                {
                    if (!request.OtroUsuarioId.HasValue)
                        return null;

                    conversacion.Usuario1Id = userId;
                    conversacion.Usuario2Id = request.OtroUsuarioId.Value;
                }
                else if (request.Tipo == "grupo")
                {
                    if (!request.GrupoId.HasValue)
                        return null;

                    // Verificar que el usuario es miembro del grupo
                    var isMember = await _context.GrupoMiembros
                        .AnyAsync(gm => gm.GrupoId == request.GrupoId && gm.UsuarioId == userId && gm.Activo);

                    if (!isMember)
                        return null;

                    conversacion.GrupoId = request.GrupoId.Value;
                }

                _context.Conversaciones.Add(conversacion);
                await _context.SaveChangesAsync();

                // Agregar participantes
                var participantes = new List<ParticipanteConversacion>();

                if (request.Tipo == "directa")
                {
                    participantes.Add(new ParticipanteConversacion
                    {
                        ConversacionId = conversacion.Id,
                        UsuarioId = userId,
                        FechaUnion = DateTime.UtcNow,
                        Activo = true
                    });

                    participantes.Add(new ParticipanteConversacion
                    {
                        ConversacionId = conversacion.Id,
                        UsuarioId = request.OtroUsuarioId!.Value,
                        FechaUnion = DateTime.UtcNow,
                        Activo = true
                    });
                }
                else if (request.Tipo == "grupo")
                {
                    // Agregar todos los miembros activos del grupo como participantes
                    var miembros = await _context.GrupoMiembros
                        .Where(gm => gm.GrupoId == request.GrupoId && gm.Activo)
                        .Select(gm => gm.UsuarioId)
                        .ToListAsync();

                    participantes.AddRange(miembros.Select(usuarioId => new ParticipanteConversacion
                    {
                        ConversacionId = conversacion.Id,
                        UsuarioId = usuarioId,
                        FechaUnion = DateTime.UtcNow,
                        Activo = true
                    }));
                }

                _context.ParticipantesConversacion.AddRange(participantes);
                await _context.SaveChangesAsync();

                return await GetConversationAsync(conversacion.Id, userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear conversación");
                return null;
            }
        }

        public async Task<ConversationDto?> GetConversationAsync(int conversationId, int userId)
        {
            try
            {
                var conversacion = await _context.Conversaciones
                    .FirstOrDefaultAsync(c => c.Id == conversationId);

                if (conversacion == null)
                    return null;

                // Verificar autorización
                if (!await IsUserInConversationAsync(conversationId, userId))
                    return null;

                // Obtener último mensaje
                var ultimoMensajeEntity = await _context.Mensajes
                    .Where(m => m.ConversacionId == conversationId && !m.Eliminado)
                    .Include(m => m.Lecturas)
                    .OrderByDescending(m => m.FechaEnvio)
                    .FirstOrDefaultAsync();

                MessageDto? ultimoMensaje = null;
                if (ultimoMensajeEntity != null)
                {
                    var remitenteNombre = await _userInfoService.GetUserNameAsync(ultimoMensajeEntity.RemitenteId);
                    ultimoMensaje = new MessageDto
                    {
                        Id = ultimoMensajeEntity.Id.ToString(),
                        ConversacionId = ultimoMensajeEntity.ConversacionId.ToString(),
                        RemitenteId = ultimoMensajeEntity.RemitenteId.ToString(),
                        RemitenteNombre = remitenteNombre,
                        Contenido = ultimoMensajeEntity.Contenido,
                        FechaEnvio = ultimoMensajeEntity.FechaEnvio,
                        Eliminado = ultimoMensajeEntity.Eliminado,
                        CantidadLecturas = ultimoMensajeEntity.Lecturas.Count,
                        LeidoPorMi = ultimoMensajeEntity.Lecturas.Any(l => l.UsuarioId == userId)
                    };
                }

                // Contar mensajes no leídos
                var mensajesNoLeidos = await _context.Mensajes
                    .Where(m => m.ConversacionId == conversationId &&
                                m.RemitenteId != userId &&
                                !m.Eliminado &&
                                !m.Lecturas.Any(l => l.UsuarioId == userId))
                    .CountAsync();

                // Obtener participantes
                var participantesIds = await _context.ParticipantesConversacion
                    .Where(pc => pc.ConversacionId == conversationId && pc.Activo)
                    .Select(pc => pc.UsuarioId)
                    .ToListAsync();

                return new ConversationDto
                {
                    Id = conversacion.Id.ToString(),
                    Tipo = conversacion.Tipo,
                    Usuario1Id = conversacion.Usuario1Id,
                    Usuario2Id = conversacion.Usuario2Id,
                    GrupoId = conversacion.GrupoId,
                    FechaCreacion = conversacion.FechaCreacion,
                    UltimoMensaje = ultimoMensaje,
                    MensajesNoLeidos = mensajesNoLeidos,
                    ParticipantesIds = participantesIds
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener conversación {ConversationId}", conversationId);
                return null;
            }
        }

        public async Task<List<ConversationDto>> GetUserConversationsAsync(int userId)
        {
            try
            {
                var conversacionesIds = await _context.ParticipantesConversacion
                    .Where(pc => pc.UsuarioId == userId && pc.Activo)
                    .Select(pc => pc.ConversacionId)
                    .ToListAsync();

                var conversaciones = new List<ConversationDto>();

                foreach (var convId in conversacionesIds)
                {
                    var conv = await GetConversationAsync(convId, userId);
                    if (conv != null)
                        conversaciones.Add(conv);
                }

                return conversaciones.OrderByDescending(c => c.UltimoMensaje?.FechaEnvio ?? c.FechaCreacion).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener conversaciones del usuario {UserId}", userId);
                return new List<ConversationDto>();
            }
        }

        public async Task<ConversationDto?> GetOrCreateDirectConversationAsync(int userId1, int userId2)
        {
            try
            {
                // Buscar conversación directa existente
                var conversacionExistente = await _context.Conversaciones
                    .FirstOrDefaultAsync(c => c.Tipo == "directa" &&
                                              ((c.Usuario1Id == userId1 && c.Usuario2Id == userId2) ||
                                               (c.Usuario1Id == userId2 && c.Usuario2Id == userId1)));

                if (conversacionExistente != null)
                    return await GetConversationAsync(conversacionExistente.Id, userId1);

                // Crear nueva conversación directa
                var request = new CreateConversationRequest
                {
                    Tipo = "directa",
                    OtroUsuarioId = userId2
                };

                return await CreateConversationAsync(userId1, request);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener o crear conversación directa entre {UserId1} y {UserId2}", userId1, userId2);
                return null;
            }
        }

        // ===== MENSAJES =====

        public async Task<MessageDto?> SendMessageAsync(int userId, SendMessageRequest request)
        {
            try
            {
                // Verificar autorización
                if (!await IsUserInConversationAsync(request.ConversacionId, userId))
                    return null;

                var mensaje = new Mensaje
                {
                    ConversacionId = request.ConversacionId,
                    RemitenteId = userId,
                    Contenido = request.Contenido,
                    FechaEnvio = DateTime.UtcNow,
                    Eliminado = false
                };

                _context.Mensajes.Add(mensaje);
                await _context.SaveChangesAsync();

                var remitenteNombre = await _userInfoService.GetUserNameAsync(userId);

                return new MessageDto
                {
                    Id = mensaje.Id.ToString(),
                    ConversacionId = mensaje.ConversacionId.ToString(),
                    RemitenteId = mensaje.RemitenteId.ToString(),
                    RemitenteNombre = remitenteNombre,
                    Contenido = mensaje.Contenido,
                    FechaEnvio = mensaje.FechaEnvio,
                    Eliminado = mensaje.Eliminado,
                    CantidadLecturas = 0,
                    LeidoPorMi = true
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al enviar mensaje");
                return null;
            }
        }

        public async Task<List<MessageDto>> GetMessagesAsync(int conversationId, int userId, int page, int pageSize)
        {
            try
            {
                // Verificar autorización
                if (!await IsUserInConversationAsync(conversationId, userId))
                    return new List<MessageDto>();

                var mensajesEntities = await _context.Mensajes
                    .Where(m => m.ConversacionId == conversationId)
                    .Include(m => m.Lecturas)
                    .OrderByDescending(m => m.FechaEnvio)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                // Obtener todos los remitentes únicos
                var remitenteIds = mensajesEntities.Select(m => m.RemitenteId).Distinct();
                var nombresRemitentes = await _userInfoService.GetUserNamesAsync(remitenteIds);

                var mensajes = mensajesEntities.Select(m => new MessageDto
                {
                    Id = m.Id.ToString(),
                    ConversacionId = m.ConversacionId.ToString(),
                    RemitenteId = m.RemitenteId.ToString(),
                    RemitenteNombre = nombresRemitentes[m.RemitenteId],
                    Contenido = m.Contenido,
                    FechaEnvio = m.FechaEnvio,
                    Eliminado = m.Eliminado,
                    CantidadLecturas = m.Lecturas.Count,
                    LeidoPorMi = m.Lecturas.Any(l => l.UsuarioId == userId)
                }).OrderBy(m => m.FechaEnvio).ToList();

                return mensajes;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener mensajes de la conversación {ConversationId}", conversationId);
                return new List<MessageDto>();
            }
        }

        public async Task<bool> DeleteMessageAsync(int messageId, int userId)
        {
            try
            {
                var mensaje = await _context.Mensajes
                    .FirstOrDefaultAsync(m => m.Id == messageId);

                if (mensaje == null || mensaje.RemitenteId != userId)
                    return false;

                mensaje.Eliminado = true;
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar mensaje {MessageId}", messageId);
                return false;
            }
        }

        // ===== READ RECEIPTS =====

        public async Task<bool> MarkMessageAsReadAsync(int messageId, int userId)
        {
            try
            {
                var mensaje = await _context.Mensajes
                    .Include(m => m.Conversacion)
                    .FirstOrDefaultAsync(m => m.Id == messageId);

                if (mensaje == null)
                    return false;

                // Verificar que el usuario está en la conversación
                if (!await IsUserInConversationAsync(mensaje.ConversacionId, userId))
                    return false;

                // Verificar si ya existe el read receipt
                var existingReceipt = await _context.MensajesLeidos
                    .FirstOrDefaultAsync(ml => ml.MensajeId == messageId && ml.UsuarioId == userId);

                if (existingReceipt != null)
                    return true; // Ya estaba marcado como leído

                var readReceipt = new MensajeLeido
                {
                    MensajeId = messageId,
                    UsuarioId = userId,
                    FechaLectura = DateTime.UtcNow
                };

                _context.MensajesLeidos.Add(readReceipt);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("duplicate") == true)
            {
                // Unique constraint violation - ya existe el read receipt
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al marcar mensaje {MessageId} como leído", messageId);
                return false;
            }
        }

        public async Task<int?> GetConversationIdByMessageIdAsync(int messageId)
        {
            try
            {
                var conversacionId = await _context.Mensajes
                    .Where(m => m.Id == messageId)
                    .Select(m => m.ConversacionId)
                    .FirstOrDefaultAsync();

                return conversacionId == 0 ? null : conversacionId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener conversationId para mensaje {MessageId}", messageId);
                return null;
            }
        }

        public async Task<List<ReadReceiptDto>> GetMessageReadReceiptsAsync(int messageId, int userId)
        {
            try
            {
                var mensaje = await _context.Mensajes
                    .Include(m => m.Conversacion)
                    .FirstOrDefaultAsync(m => m.Id == messageId);

                if (mensaje == null)
                    return new List<ReadReceiptDto>();

                // Verificar que el usuario está en la conversación
                if (!await IsUserInConversationAsync(mensaje.ConversacionId, userId))
                    return new List<ReadReceiptDto>();

                var receiptsEntities = await _context.MensajesLeidos
                    .Where(ml => ml.MensajeId == messageId)
                    .OrderBy(ml => ml.FechaLectura)
                    .ToListAsync();

                // Obtener todos los usuarios únicos
                var usuarioIds = receiptsEntities.Select(ml => ml.UsuarioId).Distinct();
                var nombresUsuarios = await _userInfoService.GetUserNamesAsync(usuarioIds);

                var receipts = receiptsEntities.Select(ml => new ReadReceiptDto
                {
                    Id = ml.Id.ToString(),
                    MensajeId = ml.MensajeId.ToString(),
                    UsuarioId = ml.UsuarioId.ToString(),
                    UsuarioNombre = nombresUsuarios[ml.UsuarioId],
                    FechaLectura = ml.FechaLectura
                }).ToList();

                return receipts;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener read receipts del mensaje {MessageId}", messageId);
                return new List<ReadReceiptDto>();
            }
        }

        // ===== AUTORIZACIÓN =====

        public async Task<bool> IsUserInConversationAsync(int conversationId, int userId)
        {
            try
            {
                var conversacion = await _context.Conversaciones
                    .FirstOrDefaultAsync(c => c.Id == conversationId);

                if (conversacion == null)
                    return false;

                // Para conversaciones directas
                if (conversacion.Tipo == "directa")
                {
                    return conversacion.Usuario1Id == userId || conversacion.Usuario2Id == userId;
                }

                // Para conversaciones de grupo
                if (conversacion.Tipo == "grupo" && conversacion.GrupoId.HasValue)
                {
                    var isMember = await _context.GrupoMiembros
                        .AnyAsync(gm => gm.GrupoId == conversacion.GrupoId && gm.UsuarioId == userId && gm.Activo);
                    return isMember;
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al verificar autorización para conversación {ConversationId}", conversationId);
                return false;
            }
        }
    }
}
