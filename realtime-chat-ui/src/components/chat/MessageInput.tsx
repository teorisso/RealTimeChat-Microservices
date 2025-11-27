import React, { useState, useRef } from 'react';
import { Send } from 'lucide-react';
import { useChatStore } from '../../store/chatStore';

const MessageInput: React.FC = () => {
    const [content, setContent] = useState('');
    const sendMessage = useChatStore((state) => state.sendMessage);
    const inputRef = useRef<HTMLInputElement>(null);

    const handleSubmit = async (e: React.FormEvent) => {
        e.preventDefault();
        if (!content.trim()) return;

        const messageToSend = content;
        setContent('');
        await sendMessage(messageToSend);
        inputRef.current?.focus();
    };

    return (
        <form onSubmit={handleSubmit} className="p-4 bg-white border-t flex items-center space-x-2">
            <input
                ref={inputRef}
                type="text"
                value={content}
                onChange={(e) => setContent(e.target.value)}
                placeholder="Type a message..."
                className="flex-1 border-gray-300 rounded-full focus:ring-blue-500 focus:border-blue-500 px-4 py-2 border"
            />
            <button
                type="submit"
                disabled={!content.trim()}
                className="p-2 bg-blue-600 text-white rounded-full hover:bg-blue-700 disabled:opacity-50 disabled:cursor-not-allowed transition-colors"
            >
                <Send className="w-5 h-5" />
            </button>
        </form>
    );
};

export default MessageInput;
