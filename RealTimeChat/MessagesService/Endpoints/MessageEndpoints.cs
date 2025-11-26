using System.Security.Claims;
using MessagesService.DTOs;
using MessagesService.Services;
using Shared.Responses;

namespace MessagesService.Endpoints
{
    public static class MessageEndpoints
    {
        public static void MapMessageEndpoints(this IEndpointRouteBuilder app)
        {
            var group = app.MapGroup("/api/messages").WithTags("Messages");

            // ===== CONVERSACIONES =====

            group.MapPost("/conversations", async (CreateConversationRequest request, IMessageService service, ClaimsPrincipal user) =>
            {
                var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!int.TryParse(userIdClaim, out var userId))
                    return Results.Unauthorized();

                var conversation = await service.CreateConversationAsync(userId, request);
                if (conversation == null)
                    return Results.BadRequest(ApiResponse<object>.FailureResponse("No se pudo crear la conversación"));

                return Results.Ok(ApiResponse<object>.SuccessResponse(conversation, "Conversación creada exitosamente"));
            })
            .RequireAuthorization()
            .WithOpenApi();

            group.MapGet("/conversations", async (IMessageService service, ClaimsPrincipal user) =>
            {
                var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!int.TryParse(userIdClaim, out var userId))
                    return Results.Unauthorized();

                var conversations = await service.GetUserConversationsAsync(userId);
                return Results.Ok(ApiResponse<object>.SuccessResponse(conversations, "Conversaciones obtenidas exitosamente"));
            })
            .RequireAuthorization()
            .WithOpenApi();

            group.MapGet("/conversations/{id}", async (int id, IMessageService service, ClaimsPrincipal user) =>
            {
                var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!int.TryParse(userIdClaim, out var userId))
                    return Results.Unauthorized();

                var conversation = await service.GetConversationAsync(id, userId);
                if (conversation == null)
                    return Results.NotFound(ApiResponse<object>.FailureResponse("Conversación no encontrada o sin acceso"));

                return Results.Ok(ApiResponse<object>.SuccessResponse(conversation, "Conversación obtenida exitosamente"));
            })
            .RequireAuthorization()
            .WithOpenApi();

            group.MapGet("/conversations/direct/{otherUserId}", async (int otherUserId, IMessageService service, ClaimsPrincipal user) =>
            {
                var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!int.TryParse(userIdClaim, out var userId))
                    return Results.Unauthorized();

                var conversation = await service.GetOrCreateDirectConversationAsync(userId, otherUserId);
                if (conversation == null)
                    return Results.BadRequest(ApiResponse<object>.FailureResponse("No se pudo obtener o crear la conversación"));

                return Results.Ok(ApiResponse<object>.SuccessResponse(conversation, "Conversación obtenida exitosamente"));
            })
            .RequireAuthorization()
            .WithOpenApi();

            // ===== MENSAJES =====

            group.MapPost("", async (SendMessageRequest request, IMessageService service, ClaimsPrincipal user) =>
            {
                var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!int.TryParse(userIdClaim, out var userId))
                    return Results.Unauthorized();

                var message = await service.SendMessageAsync(userId, request);
                if (message == null)
                    return Results.BadRequest(ApiResponse<object>.FailureResponse("No se pudo enviar el mensaje"));

                return Results.Ok(ApiResponse<object>.SuccessResponse(message, "Mensaje enviado exitosamente"));
            })
            .RequireAuthorization()
            .WithOpenApi();

            group.MapGet("/{conversationId}", async (int conversationId, int page, int pageSize, IMessageService service, ClaimsPrincipal user) =>
            {
                var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!int.TryParse(userIdClaim, out var userId))
                    return Results.Unauthorized();

                var messages = await service.GetMessagesAsync(conversationId, userId, page, pageSize);
                return Results.Ok(ApiResponse<object>.SuccessResponse(messages, "Mensajes obtenidos exitosamente"));
            })
            .RequireAuthorization()
            .WithOpenApi();

            group.MapDelete("/{messageId}", async (int messageId, IMessageService service, ClaimsPrincipal user) =>
            {
                var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!int.TryParse(userIdClaim, out var userId))
                    return Results.Unauthorized();

                var success = await service.DeleteMessageAsync(messageId, userId);
                if (!success)
                    return Results.BadRequest(ApiResponse<object>.FailureResponse("No se pudo eliminar el mensaje"));

                return Results.Ok(ApiResponse<object>.SuccessResponse(null, "Mensaje eliminado exitosamente"));
            })
            .RequireAuthorization()
            .WithOpenApi();

            // ===== READ RECEIPTS =====

            group.MapPost("/{messageId}/read", async (int messageId, IMessageService service, ClaimsPrincipal user) =>
            {
                var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!int.TryParse(userIdClaim, out var userId))
                    return Results.Unauthorized();

                var success = await service.MarkMessageAsReadAsync(messageId, userId);
                if (!success)
                    return Results.BadRequest(ApiResponse<object>.FailureResponse("No se pudo marcar el mensaje como leído"));

                return Results.Ok(ApiResponse<object>.SuccessResponse(null, "Mensaje marcado como leído"));
            })
            .RequireAuthorization()
            .WithOpenApi();

            group.MapGet("/{messageId}/receipts", async (int messageId, IMessageService service, ClaimsPrincipal user) =>
            {
                var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!int.TryParse(userIdClaim, out var userId))
                    return Results.Unauthorized();

                var receipts = await service.GetMessageReadReceiptsAsync(messageId, userId);
                return Results.Ok(ApiResponse<object>.SuccessResponse(receipts, "Read receipts obtenidos exitosamente"));
            })
            .RequireAuthorization()
            .WithOpenApi();
        }
    }
}
