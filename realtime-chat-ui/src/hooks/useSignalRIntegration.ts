import { useEffect } from 'react';
import { useAuthStore } from '../store/authStore';
import { useChatStore } from '../store/chatStore';
import SignalRService from '../services/signalrService';

export const useSignalRIntegration = () => {
    const { isAuthenticated } = useAuthStore();
    const { handleReceiveMessage, handleTypingIndicator, handleReadReceipt } = useChatStore();

    useEffect(() => {
        if (isAuthenticated) {
            const token = localStorage.getItem('accessToken');
            if (token) {
                SignalRService.startConnection(token);
            }

            SignalRService.on('ReceiveMessage', handleReceiveMessage);
            SignalRService.on('ReceiveTypingIndicator', handleTypingIndicator);
            SignalRService.on('ReceiveReadReceipt', handleReadReceipt);

            return () => {
                SignalRService.off('ReceiveMessage', handleReceiveMessage);
                SignalRService.off('ReceiveTypingIndicator', handleTypingIndicator);
                SignalRService.off('ReceiveReadReceipt', handleReadReceipt);
            };
        }
    }, [isAuthenticated, handleReceiveMessage, handleTypingIndicator, handleReadReceipt]);
};
