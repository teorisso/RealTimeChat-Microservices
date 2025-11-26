using System.Security.Claims;
using MessagesService.DTOs;
using MessagesService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Shared.DTOs;

namespace MessagesService.Hubs
{
    [Authorize]
    public class ChatHub : Hub
    {
        private readonly IMessageService _messageService;
        private readonly ILogger<ChatHub> _logger;

        public ChatHub(IMessageService messageService, ILogger<ChatHub> logger)
        {
            _messageService = messageService;
            _logger = logger;
        }

        public override async Task OnConnectedAsync()
        {
            var userId = GetUserId();
            _logger.LogInformation("Usuario {UserId} conectado al ChatHub. ConnectionId: {ConnectionId}", userId, Context.ConnectionId);
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var userId = GetUserId();
            _logger.LogInformation("Usuario {UserId} desconectado del ChatHub. ConnectionId: {ConnectionId}", userId, Context.ConnectionId);
            await base.OnDisconnectedAsync(exception);
        }

        public async Task JoinConversation(int conversacionId)
        {
            try
            {
                var userId = GetUserId();

                // Verificar autorización
                var authorized = await _messageService.IsUserInConversationAsync(conversacionId, userId);
                if (!authorized)
                {
                    _logger.LogWarning("Usuario {UserId} intentó unirse a conversación {ConversationId} sin autorización", userId, conversacionId);
                    return;
                }

                var groupName = $"conversation_{conversacionId}";
                await Groups.AddToGroupAsync(Context.ConnectionId, groupName);

                _logger.LogInformation("Usuario {UserId} se unió a conversación {ConversationId}", userId, conversacionId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al unirse a conversación {ConversationId}", conversacionId);
            }
        }

        public async Task LeaveConversation(int conversacionId)
        {
            try
            {
                var userId = GetUserId();
                var groupName = $"conversation_{conversacionId}";
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);

                _logger.LogInformation("Usuario {UserId} abandonó conversación {ConversationId}", userId, conversacionId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al abandonar conversación {ConversationId}", conversacionId);
            }
        }

        public async Task SendMessage(int conversacionId, string contenido)
        {
            try
            {
                var userId = GetUserId();

                var request = new SendMessageRequest
                {
                    ConversacionId = conversacionId,
                    Contenido = contenido
                };

                var messageDto = await _messageService.SendMessageAsync(userId, request);

                if (messageDto != null)
                {
                    var groupName = $"conversation_{conversacionId}";

                    // Broadcast el mensaje a todos en la conversación
                    await Clients.Group(groupName).SendAsync("ReceiveMessage", messageDto);

                    _logger.LogInformation("Mensaje enviado por usuario {UserId} a conversación {ConversationId}", userId, conversacionId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al enviar mensaje a conversación {ConversationId}", conversacionId);
            }
        }

        public async Task SendTypingIndicator(int conversacionId, bool isTyping)
        {
            try
            {
                var userId = GetUserId();

                // Verificar autorización
                var authorized = await _messageService.IsUserInConversationAsync(conversacionId, userId);
                if (!authorized)
                    return;

                var groupName = $"conversation_{conversacionId}";

                var typingIndicator = new TypingIndicatorDto
                {
                    ConversacionId = conversacionId.ToString(),
                    UsuarioId = userId.ToString(),
                    UsuarioNombre = "",
                    IsTyping = isTyping
                };

                // Broadcast a todos EXCEPTO al sender
                await Clients.OthersInGroup(groupName).SendAsync("ReceiveTypingIndicator", typingIndicator);

                _logger.LogDebug("Typing indicator enviado por usuario {UserId} en conversación {ConversationId}: {IsTyping}", userId, conversacionId, isTyping);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al enviar typing indicator");
            }
        }

        public async Task MarkMessageAsRead(int messageId)
        {
            try
            {
                var userId = GetUserId();

                var success = await _messageService.MarkMessageAsReadAsync(messageId, userId);

                if (success)
                {
                    // Obtener la conversación del mensaje para broadcast
                    var mensaje = await _messageService.GetMessagesAsync(0, userId, 1, 1); // Placeholder, necesitaríamos obtener el conversationId del mensaje

                    // Broadcast el read receipt
                    var readReceipt = new ReadReceiptDto
                    {
                        Id = Guid.NewGuid().ToString(),
                        MensajeId = messageId.ToString(),
                        UsuarioId = userId.ToString(),
                        UsuarioNombre = "",
                        FechaLectura = DateTime.UtcNow
                    };

                    // Note: Necesitaríamos el conversationId para hacer el broadcast correcto
                    // await Clients.Group($"conversation_{conversationId}").SendAsync("ReceiveReadReceipt", readReceipt);

                    _logger.LogInformation("Mensaje {MessageId} marcado como leído por usuario {UserId}", messageId, userId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al marcar mensaje {MessageId} como leído", messageId);
            }
        }

        private int GetUserId()
        {
            var claim = Context.User?.FindFirst(ClaimTypes.NameIdentifier);
            if (!int.TryParse(claim?.Value, out var userId))
            {
                _logger.LogError("No se pudo extraer el userId del token JWT");
                throw new UnauthorizedAccessException("Usuario no autenticado");
            }
            return userId;
        }
    }
}
