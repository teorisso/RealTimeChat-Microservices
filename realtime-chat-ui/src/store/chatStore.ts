import { create } from 'zustand';
import type { ConversationDto, MessageDto, TypingIndicatorDto, ReadReceiptDto } from '../types';
import MessageService from '../services/messageService';
import SignalRService from '../services/signalrService';

interface ChatState {
    conversations: ConversationDto[];
    activeConversationId: string | null;
    messages: MessageDto[];
    typingIndicators: { [key: string]: string[] }; // conversationId -> userNames[]
    isLoading: boolean;

    loadConversations: () => Promise<void>;
    setActiveConversation: (conversationId: string) => Promise<void>;
    sendMessage: (content: string) => Promise<void>;
    joinConversation: (conversationId: string) => Promise<void>;
    leaveConversation: (conversationId: string) => Promise<void>;

    // SignalR Event Handlers
    handleReceiveMessage: (message: MessageDto) => void;
    handleTypingIndicator: (indicator: TypingIndicatorDto) => void;
    handleReadReceipt: (receipt: ReadReceiptDto) => void;
}

export const useChatStore = create<ChatState>((set, get) => ({
    conversations: [],
    activeConversationId: null,
    messages: [],
    typingIndicators: {},
    isLoading: false,

    loadConversations: async () => {
        set({ isLoading: true });
        try {
            const conversations = await MessageService.getConversations();
            set({ conversations, isLoading: false });
        } catch (error) {
            console.error('Failed to load conversations', error);
            set({ isLoading: false });
        }
    },

    setActiveConversation: async (conversationId: string) => {
        const currentId = get().activeConversationId;
        if (currentId === conversationId) return;

        if (currentId) {
            await get().leaveConversation(currentId);
        }

        set({ activeConversationId: conversationId, messages: [], isLoading: true });

        try {
            await get().joinConversation(conversationId);
            const messages = await MessageService.getMessages(conversationId);
            set({ messages, isLoading: false });
        } catch (error) {
            console.error('Failed to load messages', error);
            set({ isLoading: false });
        }
    },

    sendMessage: async (content: string) => {
        const { activeConversationId } = get();
        if (!activeConversationId) return;

        try {
            await MessageService.sendMessage({ conversacionId: Number(activeConversationId), contenido: content });
        } catch (error) {
            console.error('Failed to send message', error);
        }
    },

    joinConversation: async (conversationId: string) => {
        try {
            await SignalRService.invoke('JoinConversation', Number(conversationId));
        } catch (error) {
            console.error('Failed to join conversation', error);
        }
    },

    leaveConversation: async (conversationId: string) => {
        try {
            await SignalRService.invoke('LeaveConversation', Number(conversationId));
        } catch (error) {
            console.error('Failed to leave conversation', error);
        }
    },

    handleReceiveMessage: (message: MessageDto) => {
        const { activeConversationId, messages, conversations } = get();

        const updatedConversations = conversations.map(c => {
            if (c.id === message.conversacionId) {
                return {
                    ...c,
                    ultimoMensaje: message,
                    mensajesNoLeidos: c.id === activeConversationId ? 0 : c.mensajesNoLeidos + 1
                };
            }
            return c;
        });

        // If conversation not found (new conversation), reload conversations or add it
        // For simplicity, we might want to reload or fetch the single conversation
        // But let's just update existing for now.

        set({ conversations: updatedConversations });

        if (activeConversationId === message.conversacionId) {
            if (!messages.find(m => m.id === message.id)) {
                set({ messages: [...messages, message] });
                SignalRService.invoke('MarkMessageAsRead', Number(message.id));
            }
        }
    },

    handleTypingIndicator: (_indicator: TypingIndicatorDto) => {
        // TODO: Implement typing indicator logic
    },

    handleReadReceipt: (receipt: ReadReceiptDto) => {
        const { messages } = get();
        const updatedMessages = messages.map(m => {
            if (m.id === receipt.mensajeId) {
                return { ...m, cantidadLecturas: m.cantidadLecturas + 1 };
            }
            return m;
        });
        set({ messages: updatedMessages });
    }
}));
