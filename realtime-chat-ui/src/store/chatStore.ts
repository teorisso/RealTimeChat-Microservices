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
    isSendingMessage: boolean; // NUEVO: Estado separado para enviar mensajes
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
    isSendingMessage: false, // NUEVO: Estado separado
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
                const { conversations: currentConversations, activeConversationId } = get();
                const mergedConversations = conversations.map(newConv => {
                    const existing = currentConversations.find(c => c.id === newConv.id);
                    if (existing) {
                        // Si es la conversación activa, SIEMPRE usar 0 (estás leyendo)
                        if (newConv.id === activeConversationId) {
                            return {
                                ...newConv,
                                mensajesNoLeidos: 0,
                                ultimoMensaje: newConv.ultimoMensaje || existing.ultimoMensaje
                            };
                        }
                        
                        // Para conversaciones NO activas:
                        // - Si el backend dice 0, confiar en él COMPLETAMENTE (mensajes fueron marcados como leídos)
                        // - Si el backend tiene un valor, comparar con el local:
                        //   * Si el último mensaje es el mismo → usar el mayor (evitar sobrescribir incrementos de SignalR)
                        //   * Si el último mensaje es diferente → confiar en backend (mensaje nuevo desde otra fuente)
                        const ultimoMensajeIgual = existing.ultimoMensaje?.id === newConv.ultimoMensaje?.id;
                        const fechaUltimoMensajeLocal = existing.ultimoMensaje?.fechaEnvio 
                            ? new Date(existing.ultimoMensaje.fechaEnvio).getTime() 
                            : 0;
                        const fechaUltimoMensajeBackend = newConv.ultimoMensaje?.fechaEnvio 
                            ? new Date(newConv.ultimoMensaje.fechaEnvio).getTime() 
                            : 0;
                        const backendTieneMensajeMasReciente = fechaUltimoMensajeBackend > fechaUltimoMensajeLocal;
                        
                        let contadorFinal: number;
                        if (newConv.mensajesNoLeidos === 0) {
                            // Backend dice 0 = confiar completamente (mensajes fueron leídos)
                            contadorFinal = 0;
                        } else if (ultimoMensajeIgual) {
                            // Mismo mensaje = usar el mayor (evitar sobrescribir incrementos de SignalR)
                            contadorFinal = Math.max(existing.mensajesNoLeidos, newConv.mensajesNoLeidos);
                        } else if (backendTieneMensajeMasReciente) {
                            // Backend tiene mensaje más reciente = confiar en backend completamente
                            contadorFinal = newConv.mensajesNoLeidos;
                        } else {
                            // Local tiene mensaje más reciente = mantener contador local pero actualizar mensaje
                            contadorFinal = existing.mensajesNoLeidos;
                        }
                        
                        // Actualizar último mensaje: usar el más reciente entre local y backend
                        const ultimoMensajeFinal = backendTieneMensajeMasReciente 
                            ? newConv.ultimoMensaje 
                            : (existing.ultimoMensaje || newConv.ultimoMensaje);
                        
                        return {
                            ...newConv,
                            mensajesNoLeidos: contadorFinal,
                            ultimoMensaje: ultimoMensajeFinal
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
            // IMPORTANTE: Solo unirse a conversaciones válidas (no drafts) y evitar duplicados
            const { activeConversationId } = get();
            const conversationsToJoin = conversations
                .filter(conv => !conv.id.startsWith('draft_')) // Excluir drafts
                .map(conv => Number(conv.id))
                .filter(id => !isNaN(id)); // Solo IDs numéricos válidos
            
            // Unirse solo si no estamos ya unidos (evitar duplicados)
            for (const convId of conversationsToJoin) {
                try {
                    await SignalRService.invoke('JoinConversation', convId);
                } catch (error) {
                    // Silently ignore - connection might not be ready yet o ya está unido
                    console.debug('Failed to join conversation', convId, error);
                }
            }
            
            // Si hay una conversación activa válida, asegurarse de estar unido
            if (activeConversationId && !activeConversationId.startsWith('draft_')) {
                const activeId = Number(activeConversationId);
                if (!isNaN(activeId) && !conversationsToJoin.includes(activeId)) {
                    try {
                        await SignalRService.invoke('JoinConversation', activeId);
                    } catch (error) {
                        console.debug('Failed to join active conversation', activeId, error);
                    }
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

        // Si es un draft, no hacer nada (solo mostrar el input)
        if (conversationId.startsWith('draft_')) {
            set({ activeConversationId: conversationId, messages: [], isLoading: false, page: 1, hasMore: false });
            return;
        }

        if (currentId && !currentId.startsWith('draft_')) {
            await get().leaveConversation(currentId);
        }

        set({ activeConversationId: conversationId, messages: [], isLoading: true, page: 1, hasMore: true });

        try {
            await get().joinConversation(conversationId);
            
            // Marcar TODOS los mensajes como leídos en background (no bloquea)
            // Esto asegura que el backend tenga el contador correcto
            MessageService.markAllAsRead(conversationId).catch(error => {
                console.warn('Failed to mark all messages as read', error);
            });
            
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
        const { activeConversationId, isSendingMessage } = get();
        if (!activeConversationId || isSendingMessage) return; // Evitar múltiples envíos

        set({ isSendingMessage: true });

        try {
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
                } finally {
                    set({ isSendingMessage: false });
                }
                return;
            }

            // Envío normal de mensaje en conversación existente
            try {
                // Use SignalR to send message - this will broadcast to all participants
                await SignalRService.invoke('SendMessage', Number(activeConversationId), content);
            } catch (error) {
                console.error('Failed to send message via SignalR', error);
                // Fallback to HTTP if SignalR fails
                try {
                    const sentMessage = await MessageService.sendMessage({ 
                        conversacionId: Number(activeConversationId), 
                        contenido: content 
                    });
                    const { messages } = get();
                    if (!messages.find(m => m.id === sentMessage.id)) {
                        set({ messages: [...messages, sentMessage] });
                    }
                } catch (httpError) {
                    console.error('HTTP fallback also failed', httpError);
                }
            } finally {
                set({ isSendingMessage: false });
            }
        } catch (error) {
            console.error('Error in sendMessage', error);
            set({ isSendingMessage: false });
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
        // Validar que no sea un draft
        if (conversationId.startsWith('draft_')) {
            return; // No hacer nada para drafts
        }
        
        const convId = Number(conversationId);
        if (isNaN(convId)) {
            console.warn('Invalid conversation ID for join:', conversationId);
            return;
        }
        
        try {
            await SignalRService.invoke('JoinConversation', convId);
        } catch (error) {
            console.error('Failed to join conversation', conversationId, error);
        }
    },

    leaveConversation: async (conversationId: string) => {
        // Validar que no sea un draft
        if (conversationId.startsWith('draft_')) {
            return; // No hacer nada para drafts
        }
        
        const convId = Number(conversationId);
        if (isNaN(convId)) {
            return; // No hacer nada para IDs inválidos
        }
        
        try {
            await SignalRService.invoke('LeaveConversation', convId);
        } catch (error) {
            console.error('Failed to leave conversation', conversationId, error);
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
                const isActive = c.id === activeConversationId;
                
                // CRÍTICO: Si es la conversación activa, el contador debe ser 0 (estás leyendo)
                // Si NO es la activa, incrementar el contador
                // PERO: Si el mensaje recibido es más reciente que el último mensaje local,
                // asegurar que se actualice el último mensaje
                const ultimoMensajeLocal = c.ultimoMensaje;
                const mensajeEsMasReciente = !ultimoMensajeLocal || 
                    new Date(message.fechaEnvio).getTime() > new Date(ultimoMensajeLocal.fechaEnvio).getTime();
                
                const nuevoContador = isActive 
                    ? 0 
                    : (c.mensajesNoLeidos + 1);
                
                // SIEMPRE actualizar el último mensaje si el mensaje recibido es más reciente
                return {
                    ...c,
                    ultimoMensaje: mensajeEsMasReciente ? message : (c.ultimoMensaje || message),
                    mensajesNoLeidos: nuevoContador
                };
            }
            return c;
        });

        // Reordenar conversaciones por último mensaje (más reciente primero)
        const sortedConversations = updatedConversations.sort((a, b) => {
            const dateA = a.ultimoMensaje?.fechaEnvio || a.fechaCreacion;
            const dateB = b.ultimoMensaje?.fechaEnvio || b.fechaCreacion;
            return new Date(dateB).getTime() - new Date(dateA).getTime();
        });

        set({ conversations: sortedConversations });

        // Si el mensaje es de la conversación activa, agregarlo a los mensajes y marcar como leído
        if (activeConversationId === message.conversacionId) {
            if (!messages.find(m => m.id === message.id)) {
                set({ messages: [...messages, message] });
                SignalRService.invoke('MarkMessageAsRead', Number(message.id)).catch(err => {
                    console.warn('Failed to mark message as read', err);
                });
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
