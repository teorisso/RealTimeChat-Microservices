import { create } from 'zustand';
import type { UsuarioDto } from '../types';
import AuthService from '../services/authService';

interface AuthState {
    user: UsuarioDto | null;
    isAuthenticated: boolean;
    isLoading: boolean;
    login: (user: UsuarioDto, token: string, refreshToken: string) => void;
    logout: () => void;
    checkAuth: () => Promise<void>;
}

export const useAuthStore = create<AuthState>((set) => ({
    user: null,
    isAuthenticated: false,
    isLoading: true,

    login: (user, token, refreshToken) => {
        localStorage.setItem('accessToken', token);
        localStorage.setItem('refreshToken', refreshToken);
        set({ user, isAuthenticated: true });
    },

    logout: () => {
        AuthService.logout();
        set({ user: null, isAuthenticated: false });
    },

    checkAuth: async () => {
        const token = localStorage.getItem('accessToken');
        if (token) {
            try {
                const user = await AuthService.getProfile();
                set({ user, isAuthenticated: true, isLoading: false });
            } catch (error) {
                set({ user: null, isAuthenticated: false, isLoading: false });
            }
        } else {
            set({ user: null, isAuthenticated: false, isLoading: false });
        }
    },
}));
