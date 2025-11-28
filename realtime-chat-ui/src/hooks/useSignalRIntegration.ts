import { useEffect } from 'react';
import { useAuthStore } from '../store/authStore';
import { useChatStore } from '../store/chatStore';
import SignalRService from '../services/signalrService';

export const useSignalRIntegration = () => {
    const { isAuthenticated } = useAuthStore();
    const { handleReceiveMessage, handleTypingIndicator, handleReadReceipt, loadConversations } = useChatStore();

    useEffect(() => {
        if (isAuthenticated) {
            const token = localStorage.getItem('accessToken');
            
            // Register event handlers first
            SignalRService.on('ReceiveMessage', handleReceiveMessage);
            SignalRService.on('ReceiveTypingIndicator', handleTypingIndicator);
            SignalRService.on('ReceiveReadReceipt', handleReadReceipt);

            if (token) {
                // Start connection, then load data
                SignalRService.startConnection(token)
                    .then(() => {
                        // Load conversations and users together
                        loadConversations();
                    })
                    .catch((err) => {
                        console.error('SignalR connection failed', err);
                        // Still try to load data even if SignalR fails
                        loadConversations();
                    });
            }

            // Polling: Recargar conversaciones cada 30 segundos para detectar nuevos grupos
            // IMPORTANTE: preserveUnreadCounts=true para no sobrescribir contadores de SignalR
            const pollingInterval = setInterval(() => {
                if (isAuthenticated) {
                    loadConversations(true); // true = preservar contadores de mensajes no leídos
                }
            }, 30000); // 30 segundos

            return () => {
                SignalRService.off('ReceiveMessage', handleReceiveMessage);
                SignalRService.off('ReceiveTypingIndicator', handleTypingIndicator);
                SignalRService.off('ReceiveReadReceipt', handleReadReceipt);
                clearInterval(pollingInterval);
            };
        }
    }, [isAuthenticated, handleReceiveMessage, handleTypingIndicator, handleReadReceipt, loadConversations]);
};
