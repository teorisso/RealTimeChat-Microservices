import React, { useState, useRef } from 'react';
import { Send, Loader2 } from 'lucide-react';
import { useChatStore } from '../../store/chatStore';

const MessageInput: React.FC = () => {
    const [message, setMessage] = useState('');
    const { sendMessage, isSendingMessage, activeConversationId, sendTypingIndicator } = useChatStore();
    const typingTimeoutRef = useRef<ReturnType<typeof setTimeout> | null>(null);

    const handleSend = async (e: React.FormEvent) => {
        e.preventDefault();
        if (!message.trim() || !activeConversationId || isSendingMessage) return;

        const messageToSend = message;
        setMessage(''); // Limpiar input inmediatamente

        await sendMessage(messageToSend);

        // Clear typing status immediately after sending
        if (typingTimeoutRef.current) {
            clearTimeout(typingTimeoutRef.current);
        }
    };

    const handleChange = (e: React.ChangeEvent<HTMLInputElement>) => {
        setMessage(e.target.value);

        if (activeConversationId) {
            // Send typing indicator
            sendTypingIndicator(activeConversationId);
        }
    };

    return (
        <form onSubmit={handleSend} className="p-4 bg-white border-t flex items-center space-x-2">
            <input
                type="text"
                value={message}
                onChange={handleChange}
                placeholder="Type a message..."
                className="flex-1 border-gray-300 rounded-full focus:ring-blue-500 focus:border-blue-500 px-4 py-2 border"
                disabled={isSendingMessage || !activeConversationId}
            />
            <button
                type="submit"
                disabled={isSendingMessage || !message.trim() || !activeConversationId}
                className="p-2 bg-blue-600 text-white rounded-full hover:bg-blue-700 disabled:opacity-50 transition-colors"
            >
                {isSendingMessage ? <Loader2 className="w-5 h-5 animate-spin" /> : <Send className="w-5 h-5" />}
            </button>
        </form>
    );
};

export default MessageInput;
