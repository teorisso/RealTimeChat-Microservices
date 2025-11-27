import React, { useEffect, useRef, useMemo } from 'react';
import { useChatStore } from '../../store/chatStore';
import { useAuthStore } from '../../store/authStore';
import MessageBubble from './MessageBubble';
import MessageInput from './MessageInput';
import MembersList from '../groups/MembersList';
import TypingIndicator from './TypingIndicator';
import { Loader2, Users, User } from 'lucide-react';

const ChatWindow: React.FC = () => {
    const { activeConversationId, messages, isLoading, conversations, loadMoreMessages, hasMore, users, loadUsers } = useChatStore();
    const { user: currentUser } = useAuthStore();
    const messagesEndRef = useRef<HTMLDivElement>(null);
    const containerRef = useRef<HTMLDivElement>(null);
    const [isFetchingMore, setIsFetchingMore] = React.useState(false);

    const activeConversation = conversations.find(c => c.id === activeConversationId);
    const isGroup = activeConversation?.tipo === 'grupo';

    useEffect(() => {
        if (users.length === 0) {
            loadUsers();
        }
    }, [users.length, loadUsers]);

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

    const conversationName = useMemo(() => {
        if (!activeConversation) return '';
        if (isGroup) return `Group ${activeConversation.grupoId}`; // Ideally fetch group name too

        const otherUserId = activeConversation.usuario1Id === Number(currentUser?.id)
            ? activeConversation.usuario2Id
            : activeConversation.usuario1Id;

        const otherUser = users.find(u => u.id === String(otherUserId));
        return otherUser?.nombre || `User ${otherUserId}`;
    }, [activeConversation, isGroup, currentUser?.id, users]);

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
                            {conversationName}
                        </h2>
                        <p className="text-sm text-gray-500">
                            {isGroup ? `${activeConversation?.participantesIds?.length || 0} members` : 'Direct message'}
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
