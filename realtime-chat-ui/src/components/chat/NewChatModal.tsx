import React, { useState, useEffect } from 'react';
import AuthService from '../../services/authService';
import MessageService from '../../services/messageService';
import { useChatStore } from '../../store/chatStore';
import { useAuthStore } from '../../store/authStore';
import { X, Loader2, Search, MessageSquare } from 'lucide-react';
import type { UsuarioDto } from '../../types';

interface NewChatModalProps {
    onClose: () => void;
}

const NewChatModal: React.FC<NewChatModalProps> = ({ onClose }) => {
    const [users, setUsers] = useState<UsuarioDto[]>([]);
    const [loading, setLoading] = useState(true);
    const [startingChatId, setStartingChatId] = useState<string | null>(null);
    const [searchTerm, setSearchTerm] = useState('');

    const { user: currentUser } = useAuthStore();
    const { loadConversations, setActiveConversation } = useChatStore();

    useEffect(() => {
        const fetchUsers = async () => {
            try {
                const allUsers = await AuthService.getUsers();
                setUsers(allUsers.filter(u => u.id !== currentUser?.id));
            } catch (error) {
                console.error('Failed to fetch users', error);
            } finally {
                setLoading(false);
            }
        };
        fetchUsers();
    }, [currentUser]);

    const handleStartChat = async (userId: string) => {
        // No crear conversación todavía, solo establecer un "draft"
        // La conversación se creará al enviar el primer mensaje
        setActiveConversation(`draft_${userId}`);
        onClose();
    };

    const filteredUsers = users.filter(u =>
        u.nombre.toLowerCase().includes(searchTerm.toLowerCase()) ||
        u.email.toLowerCase().includes(searchTerm.toLowerCase())
    );

    return (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black bg-opacity-50 p-4">
            <div className="bg-white rounded-lg shadow-xl w-full max-w-sm flex flex-col max-h-[80vh]">
                <div className="flex justify-between items-center p-4 border-b">
                    <h3 className="text-lg font-semibold text-gray-900">New Chat</h3>
                    <button onClick={onClose} className="text-gray-400 hover:text-gray-500">
                        <X className="w-5 h-5" />
                    </button>
                </div>

                <div className="p-4 border-b">
                    <div className="relative">
                        <div className="absolute inset-y-0 left-0 pl-3 flex items-center pointer-events-none">
                            <Search className="h-4 w-4 text-gray-400" />
                        </div>
                        <input
                            type="text"
                            className="block w-full pl-10 pr-3 py-2 border border-gray-300 rounded-md leading-5 bg-white placeholder-gray-500 focus:outline-none focus:placeholder-gray-400 focus:ring-1 focus:ring-blue-500 focus:border-blue-500 sm:text-sm"
                            placeholder="Search users..."
                            value={searchTerm}
                            onChange={(e) => setSearchTerm(e.target.value)}
                        />
                    </div>
                </div>

                <div className="flex-1 overflow-y-auto p-2">
                    {loading ? (
                        <div className="flex justify-center p-4"><Loader2 className="w-6 h-6 animate-spin text-blue-500" /></div>
                    ) : filteredUsers.length === 0 ? (
                        <div className="text-center p-4 text-gray-500 text-sm">No users found</div>
                    ) : (
                        <div className="space-y-1">
                            {filteredUsers.map(user => (
                                <div key={user.id}
                                    className="flex items-center justify-between p-3 hover:bg-gray-50 rounded-md cursor-pointer"
                                    onClick={() => handleStartChat(user.id)}
                                >
                                    <div className="flex items-center space-x-3">
                                        <div className="h-8 w-8 rounded-full bg-gray-200 flex items-center justify-center text-gray-600 font-medium text-xs">
                                            {user.nombre.charAt(0).toUpperCase()}
                                        </div>
                                        <div>
                                            <p className="text-sm font-medium text-gray-900">{user.nombre}</p>
                                            <p className="text-xs text-gray-500">{user.email}</p>
                                        </div>
                                    </div>
                                    <div className="text-gray-400">
                                        {startingChatId === user.id ? (
                                            <Loader2 className="w-5 h-5 animate-spin text-blue-500" />
                                        ) : (
                                            <MessageSquare className="w-5 h-5" />
                                        )}
                                    </div>
                                </div>
                            ))}
                        </div>
                    )}
                </div>
            </div>
        </div>
    );
};

export default NewChatModal;
