import { create } from 'zustand';
import type { ConversationDto, MessageDto, TypingIndicatorDto, ReadReceiptDto, UsuarioDto } from '../types';
import MessageService from '../services/messageService';
import SignalRService from '../services/signalrService';
import AuthService from '../services/authService';

interface ChatState {
    conversations: ConversationDto[];
    activeConversationId: string | null;
    messages: MessageDto[];
    users: UsuarioDto[];
    typingUsers: { conversationId: string; userId: string; userName: string }[];
    isLoading: boolean;
    page: number;
    hasMore: boolean;

    loadConversations: () => Promise<void>;
    loadUsers: () => Promise<void>;
    setActiveConversation: (conversationId: string) => Promise<void>;
    sendMessage: (content: string) => Promise<void>;
    sendTypingIndicator: (conversationId: string) => Promise<void>;
    joinConversation: (conversationId: string) => Promise<void>;
    leaveConversation: (conversationId: string) => Promise<void>;
    loadMoreMessages: () => Promise<void>;

    // SignalR Event Handlers
    handleReceiveMessage: (message: MessageDto) => void;
    handleTypingIndicator: (indicator: TypingIndicatorDto) => void;
    handleReadReceipt: (receipt: ReadReceiptDto) => void;
}

export const useChatStore = create<ChatState>((set, get) => ({
    conversations: [],
    activeConversationId: null,
    messages: [],
    users: [],
    typingUsers: [],
    isLoading: false,
    page: 1,
    hasMore: true,

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

    loadUsers: async () => {
        try {
            const users = await AuthService.getUsers();
            set({ users });
        } catch (error) {
            console.error('Failed to load users', error);
        }
    },

    setActiveConversation: async (conversationId: string) => {
        const currentId = get().activeConversationId;
        if (currentId === conversationId) return;

        if (currentId) {
            await get().leaveConversation(currentId);
        }

        set({ activeConversationId: conversationId, messages: [], isLoading: true, page: 1, hasMore: true });

        try {
            await get().joinConversation(conversationId);
            const messages = await MessageService.getMessages(conversationId, 1);
            set({ messages, isLoading: false, hasMore: messages.length === 50 });
        } catch (error) {
            console.error('Failed to load messages', error);
            set({ isLoading: false });
        }
    },

    sendMessage: async (content: string) => {
        const { activeConversationId, messages } = get();
        if (!activeConversationId) return;

        try {
            const sentMessage = await MessageService.sendMessage({ conversacionId: Number(activeConversationId), contenido: content });

            // Optimistically add message if not already present (SignalR might beat us)
            if (!messages.find(m => m.id === sentMessage.id)) {
                set({ messages: [...messages, sentMessage] });
            }
        } catch (error) {
            console.error('Failed to send message', error);
        }
    },

    sendTypingIndicator: async () => {
        // Temporarily disabled due to backend issue
        // try {
        //     await SignalRService.invoke('SendTypingIndicator', Number(conversationId));
        // } catch (error) {
        //     console.error('Failed to send typing indicator', error);
        // }
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
        const { activeConversationId, messages, conversations, loadConversations } = get();

        // Check if the conversation exists in our list
        const conversationExists = conversations.some(c => c.id === message.conversacionId);
        
        if (!conversationExists) {
            // New conversation - reload the conversations list
            loadConversations();
        } else {
            // Update existing conversation
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
            set({ conversations: updatedConversations });
        }

        if (activeConversationId === message.conversacionId) {
            if (!messages.find(m => m.id === message.id)) {
                set({ messages: [...messages, message] });
                SignalRService.invoke('MarkMessageAsRead', Number(message.id));
            }
        }
    },

    handleTypingIndicator: (indicator: TypingIndicatorDto) => {
        const { typingUsers } = get();
        const newItem = {
            conversationId: indicator.conversacionId,
            userId: indicator.usuarioId,
            userName: indicator.usuarioNombre
        };

        // Avoid duplicates
        if (!typingUsers.find(u => u.conversationId === newItem.conversationId && u.userId === newItem.userId)) {
            set({ typingUsers: [...typingUsers, newItem] });

            // Remove after 3 seconds
            setTimeout(() => {
                const { typingUsers: currentTyping } = get();
                set({
                    typingUsers: currentTyping.filter(
                        u => !(u.conversationId === newItem.conversationId && u.userId === newItem.userId)
                    )
                });
            }, 3000);
        }
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
    },

    loadMoreMessages: async () => {
        const { activeConversationId, messages, isLoading, page, hasMore } = get();
        if (!activeConversationId || isLoading || !hasMore) return;

        set({ isLoading: true });
        try {
            const nextPage = page + 1;
            const newMessages = await MessageService.getMessages(activeConversationId, nextPage);

            if (newMessages.length === 0) {
                set({ hasMore: false, isLoading: false });
            } else {
                set({
                    messages: [...newMessages, ...messages],
                    page: nextPage,
                    isLoading: false,
                    hasMore: newMessages.length === 50
                });
            }
        } catch (error) {
            console.error('Failed to load more messages', error);
            set({ isLoading: false });
        }
    }
}));
