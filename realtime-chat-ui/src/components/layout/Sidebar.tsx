import React, { useState } from 'react';
import ChatList from '../chat/ChatList';
import { useAuthStore } from '../../store/authStore';
import { LogOut, Plus, MessageSquarePlus, Users } from 'lucide-react';
import CreateGroupModal from '../groups/CreateGroupModal';
import NewChatModal from '../chat/NewChatModal';
import GroupsListModal from '../groups/GroupsListModal';

interface SidebarProps {
    onClose?: () => void;
}

const Sidebar: React.FC<SidebarProps> = ({ onClose }) => {
    const { user, logout } = useAuthStore();
    const [showCreateGroup, setShowCreateGroup] = useState(false);
    const [showNewChat, setShowNewChat] = useState(false);
    const [showGroupsList, setShowGroupsList] = useState(false);

    return (
        <div className="flex flex-col h-full">
            {/* Header */}
            <div className="px-4 py-4 border-b flex items-center justify-between bg-gray-50">
                <div className="flex items-center space-x-3">
                    <div className="h-8 w-8 rounded-full bg-blue-500 flex items-center justify-center text-white font-semibold">
                        {user?.nombre?.charAt(0).toUpperCase()}
                    </div>
                    <span className="font-medium text-gray-900 truncate max-w-[120px]">
                        {user?.nombre}
                    </span>
                </div>
                <div className="flex items-center space-x-1">
                    <button
                        onClick={() => setShowNewChat(true)}
                        className="p-2 rounded-full text-gray-500 hover:bg-gray-200"
                        title="New Chat"
                    >
                        <MessageSquarePlus className="w-5 h-5" />
                    </button>
                    <button
                        onClick={() => setShowGroupsList(true)}
                        className="p-2 rounded-full text-gray-500 hover:bg-gray-200"
                        title="My Groups"
                    >
                        <Users className="w-5 h-5" />
                    </button>
                    <button
                        onClick={() => setShowCreateGroup(true)}
                        className="p-2 rounded-full text-gray-500 hover:bg-gray-200"
                        title="New Group"
                    >
                        <Plus className="w-5 h-5" />
                    </button>
                    <button onClick={logout} className="p-2 rounded-full text-gray-500 hover:bg-gray-200" title="Logout">
                        <LogOut className="w-5 h-5" />
                    </button>
                </div>
            </div>

            {/* Chat List */}
            <div className="flex-1 overflow-y-auto">
                <ChatList onItemClick={onClose} />
            </div>

            {showCreateGroup && (
                <CreateGroupModal onClose={() => setShowCreateGroup(false)} />
            )}

            {showNewChat && (
                <NewChatModal onClose={() => setShowNewChat(false)} />
            )}

            {showGroupsList && (
                <GroupsListModal onClose={() => setShowGroupsList(false)} />
            )}
        </div>
    );
};

export default Sidebar;
