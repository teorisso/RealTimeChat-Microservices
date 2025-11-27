import api from './api';
import type { ConversationDto, MessageDto } from '../types';

const MessageService = {
    getConversations: async () => {
        const response = await api.get<ConversationDto[]>('/messages/conversations');
        return response.data;
    },

    getConversationById: async (id: string) => {
        const response = await api.get<ConversationDto>(`/messages/conversations/${id}`);
        return response.data;
    },

    getDirectConversation: async (otherUserId: string) => {
        const response = await api.get<ConversationDto>(`/messages/conversations/direct/${otherUserId}`);
        return response.data;
    },

    createConversation: async (data: { tipo: 'directa' | 'grupo'; otroUsuarioId?: number; grupoId?: number }) => {
        const response = await api.post<ConversationDto>('/messages/conversations', data);
        return response.data;
    },

    getMessages: async (conversationId: string, page: number = 1, pageSize: number = 50) => {
        const response = await api.get<MessageDto[]>(`/messages/${conversationId}`, {
            params: { page, pageSize },
        });
        return response.data;
    },

    sendMessage: async (data: { conversacionId: number; contenido: string }) => {
        const response = await api.post<MessageDto>('/messages', data);
        return response.data;
    },

    markAsRead: async (messageId: string) => {
        await api.post(`/messages/${messageId}/read`);
    },

    getMessageReceipts: async (messageId: string) => {
        const response = await api.get(`/messages/${messageId}/receipts`);
        return response.data;
    },
};

export default MessageService;
