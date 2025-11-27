import React, { useEffect, useRef } from 'react';
import { useChatStore } from '../../store/chatStore';
import MessageBubble from './MessageBubble';
import MessageInput from './MessageInput';
import { Loader2 } from 'lucide-react';

const ChatWindow: React.FC = () => {
    const { activeConversationId, messages, isLoading } = useChatStore();
    const messagesEndRef = useRef<HTMLDivElement>(null);

    const scrollToBottom = () => {
        messagesEndRef.current?.scrollIntoView({ behavior: 'smooth' });
    };

    useEffect(() => {
        scrollToBottom();
    }, [messages]);

    if (!activeConversationId) {
        return (
            <div className="flex-1 flex items-center justify-center bg-gray-50 text-gray-500">
                <p>Select a conversation to start chatting</p>
            </div>
        );
    }

    if (isLoading && messages.length === 0) {
        return (
            <div className="flex-1 flex items-center justify-center bg-gray-50">
                <Loader2 className="w-8 h-8 animate-spin text-blue-600" />
            </div>
        );
    }

    return (
        <div className="flex flex-col h-full bg-gray-50">
            {/* Messages Area */}
            <div className="flex-1 overflow-y-auto p-4 space-y-4">
                {messages.map((message) => (
                    <MessageBubble key={message.id} message={message} />
                ))}
                <div ref={messagesEndRef} />
            </div>

            {/* Input Area */}
            <MessageInput />
        </div>
    );
};

export default ChatWindow;
