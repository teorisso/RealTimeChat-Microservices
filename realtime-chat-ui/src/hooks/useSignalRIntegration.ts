import { useEffect } from 'react';
import { useAuthStore } from '../store/authStore';
import { useChatStore } from '../store/chatStore';
import SignalRService from '../services/signalrService';

export const useSignalRIntegration = () => {
    const { isAuthenticated } = useAuthStore();
    const { handleReceiveMessage, handleTypingIndicator, handleReadReceipt, loadConversations, loadUsers } = useChatStore();

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
                        // Load conversations (which will join all groups)
                        loadConversations();
                        loadUsers();
                    })
                    .catch((err) => {
                        console.error('SignalR connection failed', err);
                        // Still try to load data even if SignalR fails
                        loadConversations();
                        loadUsers();
                    });
            }

            return () => {
                SignalRService.off('ReceiveMessage', handleReceiveMessage);
                SignalRService.off('ReceiveTypingIndicator', handleTypingIndicator);
                SignalRService.off('ReceiveReadReceipt', handleReadReceipt);
            };
        }
    }, [isAuthenticated, handleReceiveMessage, handleTypingIndicator, handleReadReceipt, loadConversations, loadUsers]);
};
