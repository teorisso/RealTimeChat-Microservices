import api from './api';
import type { GrupoDto, UsuarioDto } from '../types';

const GroupService = {
    createGroup: async (data: { nombre: string; descripcion?: string; avatarUrl?: string; miembrosInicialesIds?: number[] }) => {
        const response = await api.post<GrupoDto>('/groups', data);
        return response.data;
    },

    getGroups: async () => {
        const response = await api.get<GrupoDto[]>('/groups');
        return response.data;
    },

    getGroupDetails: async (id: string) => {
        const response = await api.get<GrupoDto>(`/groups/${id}`);
        return response.data;
    },

    updateGroup: async (id: string, data: { nombre?: string; descripcion?: string; avatarUrl?: string }) => {
        const response = await api.put<GrupoDto>(`/groups/${id}`, data);
        return response.data;
    },

    deleteGroup: async (id: string) => {
        await api.delete(`/groups/${id}`);
    },

    addMember: async (groupId: string, data: { usuarioId: number; esAdmin: boolean }) => {
        await api.post(`/groups/${groupId}/members`, data);
    },

    removeMember: async (groupId: string, memberId: string) => {
        await api.delete(`/groups/${groupId}/members/${memberId}`);
    },

    getMembers: async (groupId: string) => {
        const response = await api.get<UsuarioDto[]>(`/groups/${groupId}/members`);
        return response.data;
    },
};

export default GroupService;
