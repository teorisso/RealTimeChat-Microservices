import React, { useEffect, useRef } from 'react';
import { useChatStore } from '../../store/chatStore';
import { useAuthStore } from '../../store/authStore';
import MessageBubble from './MessageBubble';
import MessageInput from './MessageInput';
import MembersList from '../groups/MembersList';
import TypingIndicator from './TypingIndicator';
import { Loader2, Users, User } from 'lucide-react';

const ChatWindow: React.FC = () => {
    const { activeConversationId, messages, isLoading, conversations, loadMoreMessages, hasMore, users } = useChatStore();
    const { user: currentUser } = useAuthStore();
    const messagesEndRef = useRef<HTMLDivElement>(null);
    const containerRef = useRef<HTMLDivElement>(null);
    const [isFetchingMore, setIsFetchingMore] = React.useState(false);

    const activeConversation = conversations.find(c => c.id === activeConversationId);
    const isGroup = activeConversation?.tipo === 'grupo';

    const scrollToBottom = () => {
        messagesEndRef.current?.scrollIntoView({ behavior: 'smooth' });
    };

    // Scroll to bottom on initial load or new message (if near bottom)
    useEffect(() => {
        if (!isFetchingMore) {
            scrollToBottom();
        }
    }, [messages.length, activeConversationId]);

    const handleScroll = async () => {
        if (!containerRef.current || isLoading || !hasMore || isFetchingMore) return;

        if (containerRef.current.scrollTop === 0) {
            setIsFetchingMore(true);
            const scrollHeightBefore = containerRef.current.scrollHeight;

            await loadMoreMessages();

            // Adjust scroll position to maintain view
            if (containerRef.current) {
                const scrollHeightAfter = containerRef.current.scrollHeight;
                containerRef.current.scrollTop = scrollHeightAfter - scrollHeightBefore;
            }
            setIsFetchingMore(false);
        }
    };

    const getOtherUser = () => {
        // Manejar conversaciones draft
        if (activeConversationId?.startsWith('draft_')) {
            const otherUserId = activeConversationId.replace('draft_', '');
            return users.find(u => u.id === otherUserId);
        }

        if (!activeConversation || isGroup) return null;

        // Try using usuario1Id/usuario2Id first
        const currentUserId = Number(currentUser?.id);
        let otherUserId = activeConversation.usuario1Id === currentUserId
            ? activeConversation.usuario2Id
            : activeConversation.usuario1Id;

        // Fallback: use participantesIds to find the other user
        if (otherUserId == null && activeConversation.participantesIds?.length) {
            otherUserId = activeConversation.participantesIds.find(id => id !== currentUserId);
        }

        if (otherUserId == null) return null;

        return users.find(u => u.id === String(otherUserId));
    };

    const getConversationName = () => {
        // Manejar conversaciones draft
        if (activeConversationId?.startsWith('draft_')) {
            const otherUser = getOtherUser();
            return otherUser?.nombre || 'New Chat';
        }

        if (!activeConversation) return '';
        if (isGroup) return activeConversation.grupoNombre || `Group ${activeConversation.grupoId}`;

        const otherUser = getOtherUser();
        return otherUser?.nombre || 'Chat';
    };

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
        <div className="flex flex-1 h-full overflow-hidden">
            <div className="flex flex-col flex-1 h-full bg-gray-50 min-w-0">
                {/* Chat Header */}
                <div className="bg-white border-b px-6 py-4 flex items-center space-x-3">
                    <div className={`h-10 w-10 rounded-full flex items-center justify-center text-white shrink-0 ${isGroup ? 'bg-indigo-500' : 'bg-gray-400'
                        }`}>
                        {isGroup ? <Users className="w-5 h-5" /> : <User className="w-5 h-5" />}
                    </div>
                    <div className="flex-1 min-w-0">
                        <h2 className="text-lg font-semibold text-gray-900 truncate">
                            {getConversationName()}
                        </h2>
                        <p className="text-sm text-gray-500">
                            {isGroup 
                                ? `${activeConversation?.participantesIds?.length || 0} members` 
                                : (getOtherUser()?.email || 'Direct message')}
                        </p>
                    </div>
                </div>

                {/* Messages Area */}
                <div
                    ref={containerRef}
                    onScroll={handleScroll}
                    className="flex-1 overflow-y-auto p-4 space-y-4"
                >
                    {isFetchingMore && (
                        <div className="flex justify-center py-2">
                            <Loader2 className="w-4 h-4 animate-spin text-gray-400" />
                        </div>
                    )}
                    {messages.map((message) => (
                        <MessageBubble key={message.id} message={message} />
                    ))}
                    <div ref={messagesEndRef} />
                </div>

                {/* Typing Indicator */}
                <TypingIndicator />

                {/* Input Area */}
                <MessageInput />
            </div>

            {/* Members List (Right Sidebar for Groups) */}
            {isGroup && activeConversation.grupoId && (
                <div className="hidden lg:block h-full">
                    <MembersList groupId={activeConversation.grupoId.toString()} />
                </div>
            )}
        </div>
    );
};

export default ChatWindow;
