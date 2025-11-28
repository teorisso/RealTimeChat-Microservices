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

    loadConversations: (preserveUnreadCounts?: boolean) => Promise<void>;
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

    loadConversations: async (preserveUnreadCounts = false) => {
        set({ isLoading: true });
        try {
            // Load users and conversations in parallel, wait for both to complete
            const [conversations, users] = await Promise.all([
                MessageService.getConversations(),
                AuthService.getUsers()
            ]);
            
            // Si preserveUnreadCounts es true, mantener los contadores actuales del estado
            if (preserveUnreadCounts) {
                const { conversations: currentConversations } = get();
                const mergedConversations = conversations.map(newConv => {
                    const existing = currentConversations.find(c => c.id === newConv.id);
                    if (existing) {
                        // Mantener el contador de no leídos del estado actual si es mayor
                        // Esto evita que el polling sobrescriba incrementos de SignalR
                        return {
                            ...newConv,
                            mensajesNoLeidos: Math.max(existing.mensajesNoLeidos, newConv.mensajesNoLeidos),
                            ultimoMensaje: newConv.ultimoMensaje || existing.ultimoMensaje
                        };
                    }
                    return newConv;
                });
                
                set({ conversations: mergedConversations, users, isLoading: false });
            } else {
                // Carga inicial o forzada - usar valores del backend
                set({ conversations, users, isLoading: false });
            }
            
            // Join all conversations for real-time updates
            for (const conv of conversations) {
                try {
                    await SignalRService.invoke('JoinConversation', Number(conv.id));
                } catch {
                    // Silently ignore - connection might not be ready yet
                }
            }
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
            
            // IMPORTANTE: Resetear el contador de no leídos INMEDIATAMENTE al abrir el chat
            const { conversations } = get();
            const updatedConversations = conversations.map(c => 
                c.id === conversationId ? { ...c, mensajesNoLeidos: 0 } : c
            );
            set({ conversations: updatedConversations });
        } catch (error) {
            console.error('Failed to load messages', error);
            set({ isLoading: false });
        }
    },

    sendMessage: async (content: string) => {
        const { activeConversationId } = get();
        if (!activeConversationId) return;

        // Detectar si es una conversación draft (directa que aún no existe)
        const isDraft = activeConversationId.startsWith('draft_');
        
        if (isDraft) {
            // Extraer el ID del otro usuario
            const otherUserId = Number(activeConversationId.replace('draft_', ''));
            
            try {
                // Enviar mensaje directo (el backend creará la conversación automáticamente)
                const sentMessage = await MessageService.sendMessage({ 
                    conversacionId: 0,  // 0 indica que no hay conversación todavía
                    contenido: content,
                    destinatarioId: otherUserId 
                });
                
                if (sentMessage) {
                    // Recargar conversaciones para obtener la recién creada
                    await get().loadConversations();
                    
                    // Cambiar a la conversación real
                    set({ 
                        activeConversationId: sentMessage.conversacionId,
                        messages: [sentMessage]
                    });
                    
                    // Unirse a la conversación en SignalR
                    await SignalRService.invoke('JoinConversation', Number(sentMessage.conversacionId));
                }
            } catch (error) {
                console.error('Failed to send draft message', error);
            }
            return;
        }

        // Envío normal de mensaje en conversación existente
        try {
            // Use SignalR to send message - this will broadcast to all participants
            await SignalRService.invoke('SendMessage', Number(activeConversationId), content);
        } catch (error) {
            console.error('Failed to send message', error);
            // Fallback to HTTP if SignalR fails
            try {
                const sentMessage = await MessageService.sendMessage({ conversacionId: Number(activeConversationId), contenido: content });
                const { messages } = get();
                if (!messages.find(m => m.id === sentMessage.id)) {
                    set({ messages: [...messages, sentMessage] });
                }
            } catch (httpError) {
                console.error('HTTP fallback also failed', httpError);
            }
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

    handleReceiveMessage: async (message: MessageDto) => {
        const { activeConversationId, messages, conversations } = get();

        // Verificar si la conversación existe en el estado actual
        const conversationExists = conversations.some(c => c.id === message.conversacionId);
        
        if (!conversationExists) {
            // Si la conversación no existe (ej: te agregaron a un grupo nuevo), recargar todas las conversaciones
            console.log('Nueva conversación detectada, recargando lista...');
            await get().loadConversations();
            return;
        }

        // Actualizar la conversación existente
        const updatedConversations = conversations.map(c => {
            if (c.id === message.conversacionId) {
                // Si es la conversación activa, no incrementar no leídos
                // Si no es la activa, calcular correctamente los no leídos
                const isActive = c.id === activeConversationId;
                return {
                    ...c,
                    ultimoMensaje: message,
                    mensajesNoLeidos: isActive ? 0 : c.mensajesNoLeidos + 1
                };
            }
            return c;
        });

        // Reordenar conversaciones por último mensaje
        const sortedConversations = updatedConversations.sort((a, b) => {
            const dateA = a.ultimoMensaje?.fechaEnvio || a.fechaCreacion;
            const dateB = b.ultimoMensaje?.fechaEnvio || b.fechaCreacion;
            return new Date(dateB).getTime() - new Date(dateA).getTime();
        });

        set({ conversations: sortedConversations });

        // Si el mensaje es de la conversación activa, agregarlo a los mensajes
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
