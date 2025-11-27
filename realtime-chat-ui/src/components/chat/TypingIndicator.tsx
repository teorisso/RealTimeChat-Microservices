import React from 'react';
import { useChatStore } from '../../store/chatStore';

const TypingIndicator: React.FC = () => {
    const { typingUsers, activeConversationId, conversations } = useChatStore();

    // Filter typing users for the current conversation
    const activeTypingUsers = typingUsers.filter(
        (u) => u.conversationId === activeConversationId
    );

    if (activeTypingUsers.length === 0) return null;

    const activeConversation = conversations.find(c => c.id === activeConversationId);
    const isGroup = activeConversation?.tipo === 'grupo';

    let text = '';
    if (activeTypingUsers.length === 1) {
        text = isGroup
            ? `${activeTypingUsers[0].userName} is typing...`
            : 'typing...';
    } else if (activeTypingUsers.length === 2) {
        text = `${activeTypingUsers[0].userName} and ${activeTypingUsers[1].userName} are typing...`;
    } else {
        text = 'Several people are typing...';
    }

    return (
        <div className="px-4 py-2 text-xs text-gray-500 italic animate-pulse">
            {text}
        </div>
    );
};

export default TypingIndicator;
