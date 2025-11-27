import api from './api';
import type { AuthResponse, UsuarioDto } from '../types';

const AuthService = {
    login: async (credentials: { email: string; password: string }) => {
        const response = await api.post<AuthResponse>('/auth/login', credentials);
        return response.data;
    },

    register: async (data: { nombre: string; email: string; password: string; confirmPassword: string }) => {
        const response = await api.post<AuthResponse>('/auth/register', data);
        return response.data;
    },

    logout: async () => {
        try {
            await api.post('/auth/logout');
        } catch (error) {
            console.error('Logout failed', error);
        } finally {
            localStorage.removeItem('accessToken');
            localStorage.removeItem('refreshToken');
        }
    },

    getProfile: async () => {
        const response = await api.get<UsuarioDto>('/auth/profile');
        return response.data;
    },

    updateProfile: async (data: { nombre?: string; avatarUrl?: string }) => {
        const response = await api.put<UsuarioDto>('/auth/profile', data);
        return response.data;
    },

    getUsers: async () => {
        const response = await api.get<UsuarioDto[]>('/auth/users');
        return response.data;
    },
};

export default AuthService;
