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
    const { conversations, activeConversationId, loadConversations, setActiveConversation, isLoading, users, loadUsers } = useChatStore();
    const { user } = useAuthStore();

    useEffect(() => {
        loadConversations();
        if (users.length === 0) {
            loadUsers();
        }
    }, [loadConversations, loadUsers, users.length]);

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
                
                // Get the display name from loaded users
                const otherUser = users.find(u => u.id === String(otherUserId));
                const displayName = isGroup ? `Group ${chat.grupoId}` : (otherUser?.nombre || `User ${otherUserId}`);

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
