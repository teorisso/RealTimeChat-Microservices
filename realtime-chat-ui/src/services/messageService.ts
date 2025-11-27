import { messagesApi } from './api';
import type { ConversationDto, MessageDto, ReadReceiptDto } from '../types';

const MessageService = {
    getConversations: async () => {
        const response = await messagesApi.get<{ success: boolean; data: ConversationDto[] }>('/messages/conversations');
        return response.data.data || [];
    },

    getConversationById: async (id: string) => {
        const response = await messagesApi.get<{ success: boolean; data: ConversationDto }>(`/messages/conversations/${id}`);
        return response.data.data!;
    },

    getDirectConversation: async (otherUserId: string) => {
        const response = await messagesApi.get<{ success: boolean; data: ConversationDto }>(`/messages/conversations/direct/${otherUserId}`);
        return response.data.data!;
    },

    createConversation: async (data: { tipo: 'directa' | 'grupo'; otroUsuarioId?: number; grupoId?: number }) => {
        const response = await messagesApi.post<{ success: boolean; data: ConversationDto }>('/messages/conversations', data);
        return response.data.data!;
    },

    getMessages: async (conversationId: string, page: number = 1, pageSize: number = 50) => {
        const response = await messagesApi.get<{ success: boolean; data: MessageDto[] }>(`/messages/${conversationId}`, {
            params: { page, pageSize },
        });
        return response.data.data || [];
    },

    sendMessage: async (data: { conversacionId: number; contenido: string }) => {
        const response = await messagesApi.post<{ success: boolean; data: MessageDto }>('/messages', data);
        return response.data.data!;
    },

    markAsRead: async (messageId: string) => {
        await messagesApi.post(`/messages/${messageId}/read`);
    },

    getMessageReceipts: async (messageId: string) => {
        const response = await messagesApi.get<{ success: boolean; data: ReadReceiptDto[] }>(`/messages/${messageId}/receipts`);
        return response.data.data || [];
    },
};

export default MessageService;
