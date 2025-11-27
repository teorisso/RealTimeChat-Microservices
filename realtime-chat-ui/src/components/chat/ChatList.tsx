import React, { useEffect } from 'react';
import { useChatStore } from '../../store/chatStore';
import { useAuthStore } from '../../store/authStore';
import { format } from 'date-fns';
import { Users, User } from 'lucide-react';
import clsx from 'clsx';

interface ChatListProps {
    onItemClick?: () => void;
}

const ChatList: React.FC<ChatListProps> = ({ onItemClick }) => {
    const { conversations, activeConversationId, loadConversations, setActiveConversation, isLoading } = useChatStore();
    const { user } = useAuthStore();

    useEffect(() => {
        loadConversations();
    }, [loadConversations]);

    const handleSelect = (id: string) => {
        setActiveConversation(id);
        if (onItemClick) onItemClick();
    };

    if (isLoading && conversations.length === 0) {
        return <div className="p-4 text-center text-gray-500">Loading chats...</div>;
    }

    return (
        <div className="divide-y divide-gray-100">
            {conversations.map((chat) => {
                const isGroup = chat.tipo === 'grupo';
                const otherUserId = chat.usuario1Id === Number(user?.id) ? chat.usuario2Id : chat.usuario1Id;
                // In a real app, we'd fetch the other user's name or store it in the conversation DTO better.
                // The DTO has 'participantesIds', but not names directly unless we fetch or it's in a view model.
                // Wait, the DTO provided in user request:
                // interface ConversationDto { ... usuario1Id, usuario2Id, grupoId ... }
                // It doesn't have the name of the other user directly.
                // However, the backend might return enriched data or we need to fetch users.
                // Let's assume for now we might need to fetch or the backend provides it.
                // Actually, looking at the backend endpoints, `GET /api/messages/conversations` returns `ConversationDto`.
                // If it doesn't have names, we might need to fetch user details or the backend should provide it.
                // Let's assume for this implementation we might display "Chat {id}" or "Group {id}" if name is missing,
                // but ideally the backend should return it.
                // Wait, `ConversationDto` has `ultimoMensaje` which has `remitenteNombre`.
                // But for the chat title, if it's direct, we need the other user's name.
                // If it's a group, we need the group name.
                // The `ConversationDto` has `grupoId`. If it's a group, we can fetch group details.
                // If it's direct, we can fetch user details.
                // Or maybe the backend returns a View Model with the name.
                // Let's assume for now we display "Conversation" or try to find a name.

                // For the sake of the demo, I'll assume the backend might be adjusted or I'll just use IDs/Generic names if missing.
                // Actually, `ConversationDto` in the user request doesn't have a name field for direct chats.
                // But `GrupoDto` has `nombre`.
                // Maybe I should fetch group details if `grupoId` is present.
                // For direct, I might need to fetch `GET /api/auth/users/{id}`.
                // To avoid N+1, I'll just show "User {id}" for now or implement a cache.

                const displayName = isGroup ? `Group ${chat.grupoId}` : `User ${otherUserId}`;
                // Ideally we'd have the name.

                return (
                    <button
                        key={chat.id}
                        onClick={() => handleSelect(chat.id)}
                        className={clsx(
                            "w-full px-4 py-3 flex items-center space-x-3 hover:bg-gray-50 transition-colors text-left focus:outline-none",
                            activeConversationId === chat.id && "bg-blue-50 hover:bg-blue-50"
                        )}
                    >
                        <div className={clsx(
                            "h-10 w-10 rounded-full flex items-center justify-center text-white shrink-0",
                            isGroup ? "bg-indigo-500" : "bg-gray-400"
                        )}>
                            {isGroup ? <Users className="w-5 h-5" /> : <User className="w-5 h-5" />}
                        </div>

                        <div className="flex-1 min-w-0">
                            <div className="flex justify-between items-baseline">
                                <h3 className="text-sm font-medium text-gray-900 truncate">
                                    {displayName}
                                </h3>
                                {chat.ultimoMensaje && (
                                    <span className="text-xs text-gray-500">
                                        {format(new Date(chat.ultimoMensaje.fechaEnvio), 'HH:mm')}
                                    </span>
                                )}
                            </div>
                            <p className="text-sm text-gray-500 truncate">
                                {chat.ultimoMensaje ? (
                                    <>
                                        <span className="font-medium text-gray-900">{chat.ultimoMensaje.remitenteNombre}: </span>
                                        {chat.ultimoMensaje.contenido}
                                    </>
                                ) : (
                                    <span className="italic">No messages yet</span>
                                )}
                            </p>
                        </div>

                        {chat.mensajesNoLeidos > 0 && (
                            <div className="bg-blue-600 text-white text-xs font-bold px-2 py-0.5 rounded-full">
                                {chat.mensajesNoLeidos}
                            </div>
                        )}
                    </button>
                );
            })}
        </div>
    );
};

export default ChatList;
