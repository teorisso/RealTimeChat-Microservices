import { groupsApi } from './api';
import type { GrupoDto, UsuarioDto } from '../types';

const GroupService = {
    createGroup: async (data: { nombre: string; descripcion?: string; avatarUrl?: string; miembrosInicialesIds?: number[] }) => {
        const response = await groupsApi.post<{ success: boolean; data: GrupoDto }>('/groups', data);
        return response.data.data!;
    },

    getGroups: async () => {
        const response = await groupsApi.get<{ success: boolean; data: GrupoDto[] }>('/groups');
        return response.data.data || [];
    },

    getGroupDetails: async (id: string) => {
        const response = await groupsApi.get<{ success: boolean; data: GrupoDto }>(`/groups/${id}`);
        return response.data.data!;
    },

    updateGroup: async (id: string, data: { nombre?: string; descripcion?: string; avatarUrl?: string }) => {
        const response = await groupsApi.put<{ success: boolean; data: GrupoDto }>(`/groups/${id}`, data);
        return response.data.data!;
    },

    deleteGroup: async (id: string) => {
        await groupsApi.delete(`/groups/${id}`);
    },

    addMember: async (groupId: string, data: { usuarioId: number; esAdmin: boolean }) => {
        await groupsApi.post(`/groups/${groupId}/members`, data);
    },

    removeMember: async (groupId: string, memberId: string) => {
        await groupsApi.delete(`/groups/${groupId}/members/${memberId}`);
    },

    getMembers: async (groupId: string) => {
        const response = await groupsApi.get<{ success: boolean; data: UsuarioDto[] }>(`/groups/${groupId}/members`);
        return response.data.data || [];
    },
};

export default GroupService;
