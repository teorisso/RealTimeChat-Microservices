import { authApi } from './api';
import type { AuthResponse, UsuarioDto } from '../types';

const AuthService = {
    login: async (credentials: { email: string; password: string }) => {
        const response = await authApi.post<AuthResponse>('/auth/login', credentials);
        return response.data;
    },

    register: async (data: { nombre: string; email: string; password: string; confirmPassword: string }) => {
        const response = await authApi.post<AuthResponse>('/auth/register', data);
        return response.data;
    },

    logout: async () => {
        try {
            await authApi.post('/auth/logout');
        } catch (error) {
            console.error('Logout failed', error);
        } finally {
            localStorage.removeItem('accessToken');
            localStorage.removeItem('refreshToken');
            localStorage.removeItem('user');
        }
    },

    getProfile: async () => {
        const response = await authApi.get<{ success: boolean; data: UsuarioDto }>('/auth/profile');
        return response.data.data!;
    },

    updateProfile: async (data: { nombre?: string; avatarUrl?: string }) => {
        await authApi.put('/auth/profile', data);
    },

    getUsers: async () => {
        const response = await authApi.get<{ success: boolean; data: UsuarioDto[] }>('/auth/users');
        return response.data.data || [];
    },
};

export default AuthService;
