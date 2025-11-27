import React, { useEffect, useState } from 'react';
import GroupService from '../../services/groupService';
import { useAuthStore } from '../../store/authStore';
import { useChatStore } from '../../store/chatStore';
import { Loader2, Trash2, Shield, UserPlus } from 'lucide-react';
import type { UsuarioDto, GrupoDto } from '../../types';
import AddMemberModal from './AddMemberModal';

interface MembersListProps {
    groupId: string;
}

const MembersList: React.FC<MembersListProps> = ({ groupId }) => {
    const [members, setMembers] = useState<UsuarioDto[]>([]);
    const [groupDetails, setGroupDetails] = useState<GrupoDto | null>(null);
    const [loading, setLoading] = useState(true);
    const [showAddMember, setShowAddMember] = useState(false);
    const { user: currentUser } = useAuthStore();
    const { loadConversations } = useChatStore();

    const fetchMembers = async () => {
        try {
            const [membersData, groupData] = await Promise.all([
                GroupService.getMembers(groupId),
                GroupService.getGroupDetails(groupId)
            ]);
            setMembers(membersData);
            setGroupDetails(groupData);
        } catch (error) {
            console.error('Failed to fetch members', error);
        } finally {
            setLoading(false);
        }
    };

    useEffect(() => {
        fetchMembers();
    }, [groupId]);

    const handleRemoveMember = async (memberId: string) => {
        if (!confirm('Are you sure you want to remove this member?')) return;
        try {
            await GroupService.removeMember(groupId, memberId);
            await fetchMembers();
            await loadConversations(); // Refresh member count in chat list
        } catch (error) {
            console.error('Failed to remove member', error);
        }
    };

    const isCreator = groupDetails?.creadorId === currentUser?.id;

    if (loading) {
        return <div className="flex justify-center p-4"><Loader2 className="w-5 h-5 animate-spin text-gray-400" /></div>;
    }

    return (
        <div className="border-l bg-white w-64 flex flex-col h-full">
            <div className="p-4 border-b flex justify-between items-center bg-gray-50">
                <h3 className="font-semibold text-gray-900">Group Members</h3>
                <span className="text-xs bg-gray-200 text-gray-600 px-2 py-1 rounded-full">{members.length}</span>
            </div>

            <div className="flex-1 overflow-y-auto p-2 space-y-1">
                {members.map((member) => (
                    <div key={member.id} className="flex items-center justify-between p-2 hover:bg-gray-50 rounded-md group">
                        <div className="flex items-center space-x-2 min-w-0">
                            <div className="h-8 w-8 rounded-full bg-indigo-100 flex items-center justify-center text-indigo-600 font-medium text-xs shrink-0">
                                {member.nombre.charAt(0).toUpperCase()}
                            </div>
                            <div className="min-w-0">
                                <p className="text-sm font-medium text-gray-900 truncate">{member.nombre}</p>
                                {member.id === groupDetails?.creadorId && (
                                    <span className="text-[10px] text-indigo-600 flex items-center">
                                        <Shield className="w-3 h-3 mr-1" /> Admin
                                    </span>
                                )}
                            </div>
                        </div>

                        {isCreator && member.id !== currentUser?.id && (
                            <button
                                onClick={() => handleRemoveMember(member.id)}
                                className="text-gray-400 hover:text-red-500 opacity-0 group-hover:opacity-100 transition-opacity p-1"
                                title="Remove member"
                            >
                                <Trash2 className="w-4 h-4" />
                            </button>
                        )}
                    </div>
                ))}
            </div>

            {isCreator && (
                <div className="p-4 border-t bg-gray-50">
                    <button
                        onClick={() => setShowAddMember(true)}
                        className="w-full flex items-center justify-center px-4 py-2 border border-transparent text-sm font-medium rounded-md text-white bg-indigo-600 hover:bg-indigo-700 shadow-sm"
                    >
                        <UserPlus className="w-4 h-4 mr-2" />
                        Add Member
                    </button>
                </div>
            )}

            {showAddMember && (
                <AddMemberModal
                    groupId={groupId}
                    currentMembers={members}
                    onClose={() => setShowAddMember(false)}
                    onSuccess={fetchMembers}
                />
            )}
        </div>
    );
};

export default MembersList;
