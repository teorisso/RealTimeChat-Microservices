using MessagesService.DTOs;
using Shared.DTOs;

namespace MessagesService.Services
{
    public interface IMessageService
    {
        // Conversations
        Task<ConversationDto?> CreateConversationAsync(int userId, CreateConversationRequest request);
        Task<ConversationDto?> GetConversationAsync(int conversationId, int userId);
        Task<List<ConversationDto>> GetUserConversationsAsync(int userId);
        Task<ConversationDto?> GetOrCreateDirectConversationAsync(int userId1, int userId2);

        // Messages
        Task<MessageDto?> SendMessageAsync(int userId, SendMessageRequest request);
        Task<List<MessageDto>> GetMessagesAsync(int conversationId, int userId, int page, int pageSize);
        Task<bool> DeleteMessageAsync(int messageId, int userId);

        // Read Receipts
        Task<bool> MarkMessageAsReadAsync(int messageId, int userId);
        Task<List<ReadReceiptDto>> GetMessageReadReceiptsAsync(int messageId, int userId);
        Task<int?> GetConversationIdByMessageIdAsync(int messageId);

        // Authorization
        Task<bool> IsUserInConversationAsync(int conversationId, int userId);
    }
}
