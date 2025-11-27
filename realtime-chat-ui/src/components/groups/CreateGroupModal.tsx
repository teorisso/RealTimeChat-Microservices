import React, { useState, useEffect } from 'react';
import GroupService from '../../services/groupService';
import AuthService from '../../services/authService';
import { useChatStore } from '../../store/chatStore';
import { useAuthStore } from '../../store/authStore';
import { X, Loader2, Check } from 'lucide-react';
import type { UsuarioDto } from '../../types';
import clsx from 'clsx';

interface CreateGroupModalProps {
    onClose: () => void;
}

const CreateGroupModal: React.FC<CreateGroupModalProps> = ({ onClose }) => {
    const [name, setName] = useState('');
    const [description, setDescription] = useState('');
    const [loading, setLoading] = useState(false);
    const [users, setUsers] = useState<UsuarioDto[]>([]);
    const [selectedUserIds, setSelectedUserIds] = useState<number[]>([]);
    const { loadConversations } = useChatStore();
    const { user: currentUser } = useAuthStore();

    useEffect(() => {
        const fetchUsers = async () => {
            try {
                const allUsers = await AuthService.getUsers();
                // Filter out current user
                setUsers(allUsers.filter(u => u.id !== currentUser?.id));
            } catch (error) {
                console.error('Failed to fetch users', error);
            }
        };
        fetchUsers();
    }, [currentUser]);

    const toggleUser = (userId: string) => {
        const id = Number(userId);
        setSelectedUserIds(prev =>
            prev.includes(id)
                ? prev.filter(uid => uid !== id)
                : [...prev, id]
        );
    };

    const handleSubmit = async (e: React.FormEvent) => {
        e.preventDefault();
        if (!name.trim()) return;

        setLoading(true);
        try {
            await GroupService.createGroup({
                nombre: name,
                descripcion: description,
                miembrosInicialesIds: selectedUserIds
            });
            await loadConversations();
            onClose();
        } catch (error) {
            console.error('Failed to create group', error);
        } finally {
            setLoading(false);
        }
    };

    return (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black bg-opacity-50 p-4">
            <div className="bg-white rounded-lg shadow-xl w-full max-w-md flex flex-col max-h-[90vh]">
                <div className="flex justify-between items-center p-6 border-b">
                    <h3 className="text-lg font-semibold text-gray-900">Create New Group</h3>
                    <button onClick={onClose} className="text-gray-400 hover:text-gray-500">
                        <X className="w-5 h-5" />
                    </button>
                </div>

                <form onSubmit={handleSubmit} className="flex-1 flex flex-col overflow-hidden">
                    <div className="p-6 overflow-y-auto flex-1 space-y-4">
                        <div>
                            <label htmlFor="groupName" className="block text-sm font-medium text-gray-700 mb-1">
                                Group Name
                            </label>
                            <input
                                id="groupName"
                                type="text"
                                value={name}
                                onChange={(e) => setName(e.target.value)}
                                className="w-full border-gray-300 rounded-md shadow-sm focus:ring-blue-500 focus:border-blue-500 px-3 py-2 border"
                                placeholder="My Awesome Group"
                                required
                            />
                        </div>

                        <div>
                            <label htmlFor="description" className="block text-sm font-medium text-gray-700 mb-1">
                                Description (Optional)
                            </label>
                            <input
                                id="description"
                                type="text"
                                value={description}
                                onChange={(e) => setDescription(e.target.value)}
                                className="w-full border-gray-300 rounded-md shadow-sm focus:ring-blue-500 focus:border-blue-500 px-3 py-2 border"
                                placeholder="What's this group about?"
                            />
                        </div>

                        <div>
                            <label className="block text-sm font-medium text-gray-700 mb-2">
                                Add Members
                            </label>
                            <div className="border rounded-md divide-y max-h-48 overflow-y-auto">
                                {users.length === 0 ? (
                                    <div className="p-4 text-center text-gray-500 text-sm">No users found</div>
                                ) : (
                                    users.map(u => (
                                        <div
                                            key={u.id}
                                            className={clsx(
                                                "flex items-center justify-between p-3 cursor-pointer hover:bg-gray-50 transition-colors",
                                                selectedUserIds.includes(Number(u.id)) && "bg-blue-50 hover:bg-blue-50"
                                            )}
                                            onClick={() => toggleUser(u.id)}
                                        >
                                            <div className="flex items-center space-x-3">
                                                <div className="h-8 w-8 rounded-full bg-gray-200 flex items-center justify-center text-gray-600 font-medium text-xs">
                                                    {u.nombre.charAt(0).toUpperCase()}
                                                </div>
                                                <span className="text-sm font-medium text-gray-900">{u.nombre}</span>
                                            </div>
                                            {selectedUserIds.includes(Number(u.id)) && (
                                                <Check className="w-4 h-4 text-blue-600" />
                                            )}
                                        </div>
                                    ))
                                )}
                            </div>
                            <p className="text-xs text-gray-500 mt-1">
                                {selectedUserIds.length} members selected
                            </p>
                        </div>
                    </div>

                    <div className="p-6 border-t bg-gray-50 flex justify-end space-x-3">
                        <button
                            type="button"
                            onClick={onClose}
                            className="px-4 py-2 text-sm font-medium text-gray-700 bg-white border border-gray-300 hover:bg-gray-50 rounded-md shadow-sm"
                        >
                            Cancel
                        </button>
                        <button
                            type="submit"
                            disabled={loading || !name.trim()}
                            className="px-4 py-2 text-sm font-medium text-white bg-blue-600 hover:bg-blue-700 rounded-md disabled:opacity-50 flex items-center shadow-sm"
                        >
                            {loading && <Loader2 className="w-4 h-4 mr-2 animate-spin" />}
                            Create Group
                        </button>
                    </div>
                </form>
            </div>
        </div>
    );
};

export default CreateGroupModal;
