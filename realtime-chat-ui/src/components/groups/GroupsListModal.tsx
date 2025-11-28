import React, { useState, useEffect } from 'react';
import GroupService from '../../services/groupService';
import MessageService from '../../services/messageService';
import { useChatStore } from '../../store/chatStore';
import { X, Loader2, Users, MessageSquare } from 'lucide-react';
import type { GrupoDto } from '../../types';

interface GroupsListModalProps {
    onClose: () => void;
}

const GroupsListModal: React.FC<GroupsListModalProps> = ({ onClose }) => {
    const [groups, setGroups] = useState<GrupoDto[]>([]);
    const [loading, setLoading] = useState(true);
    const [startingChatId, setStartingChatId] = useState<number | null>(null);

    const { loadConversations, setActiveConversation } = useChatStore();

    useEffect(() => {
        const fetchGroups = async () => {
            try {
                const userGroups = await GroupService.getGroups();
                setGroups(userGroups);
            } catch (error) {
                console.error('Failed to fetch groups', error);
            } finally {
                setLoading(false);
            }
        };
        fetchGroups();
    }, []);

    const handleStartGroupChat = async (groupId: number) => {
        // Evitar múltiples clics
        if (startingChatId !== null) return;
        
        setStartingChatId(groupId);
        try {
            // Recargar conversaciones para asegurarnos de tener las más recientes
            await loadConversations();
            
            // Buscar la conversación existente para este grupo
            const { conversations } = useChatStore.getState();
            const conversation = conversations.find(c => c.grupoId === groupId);
            
            if (conversation) {
                // La conversación ya existe (debería existir desde que se creó el grupo)
                setActiveConversation(conversation.id);
                onClose();
            } else {
                // FALLBACK: Si no existe, crearla (no debería pasar normalmente)
                console.warn('Conversación de grupo no encontrada, creando fallback para grupoId:', groupId);
                const newConversation = await MessageService.createConversation({
                    tipo: 'grupo',
                    grupoId: groupId
                });
                
                await loadConversations();
                setActiveConversation(newConversation.id);
                onClose();
            }
        } catch (error) {
            console.error('Failed to start group chat', error);
        } finally {
            setStartingChatId(null);
        }
    };

    return (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black bg-opacity-50 p-4">
            <div className="bg-white rounded-lg shadow-xl w-full max-w-md flex flex-col max-h-[80vh]">
                <div className="flex justify-between items-center p-4 border-b">
                    <h3 className="text-lg font-semibold text-gray-900">Mis Grupos</h3>
                    <button onClick={onClose} className="text-gray-400 hover:text-gray-500">
                        <X className="w-5 h-5" />
                    </button>
                </div>

                <div className="flex-1 overflow-y-auto p-2">
                    {loading ? (
                        <div className="flex justify-center p-4">
                            <Loader2 className="w-6 h-6 animate-spin text-blue-500" />
                        </div>
                    ) : groups.length === 0 ? (
                        <div className="text-center p-8">
                            <Users className="w-12 h-12 mx-auto text-gray-300 mb-3" />
                            <p className="text-gray-500 text-sm">No perteneces a ningún grupo</p>
                            <p className="text-gray-400 text-xs mt-1">Pídele a alguien que te agregue a un grupo</p>
                        </div>
                    ) : (
                        <div className="space-y-1">
                            {groups.map(group => (
                                <div 
                                    key={group.id}
                                    className="flex items-center justify-between p-3 hover:bg-gray-50 rounded-md cursor-pointer"
                                    onClick={() => handleStartGroupChat(Number(group.id))}
                                >
                                    <div className="flex items-center space-x-3 flex-1">
                                        <div className="h-10 w-10 rounded-full bg-gradient-to-br from-blue-400 to-blue-600 flex items-center justify-center text-white font-medium text-sm">
                                            {group.nombre.charAt(0).toUpperCase()}
                                        </div>
                                        <div className="flex-1 min-w-0">
                                            <p className="text-sm font-medium text-gray-900 truncate">
                                                {group.nombre}
                                            </p>
                                            {group.descripcion && (
                                                <p className="text-xs text-gray-500 truncate">
                                                    {group.descripcion}
                                                </p>
                                            )}
                                            <p className="text-xs text-gray-400 mt-0.5">
                                                {group.cantidadMiembros || 0} miembros
                                            </p>
                                        </div>
                                    </div>
                                    <div className="text-gray-400 ml-2">
                                        {startingChatId === Number(group.id) ? (
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

export default GroupsListModal;

