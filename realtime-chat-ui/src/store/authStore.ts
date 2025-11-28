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
        localStorage.setItem('user', JSON.stringify(user));
        set({ user, isAuthenticated: true, isLoading: false });
    },

    logout: () => {
        AuthService.logout();
        set({ user: null, isAuthenticated: false });
    },

    checkAuth: async () => {
        const token = localStorage.getItem('accessToken');
        const savedUser = localStorage.getItem('user');
        
        // Cargar usuario de localStorage inmediatamente
        if (savedUser) {
            set({ user: JSON.parse(savedUser), isAuthenticated: !!token, isLoading: true });
        }
        
        if (token) {
            try {
                const user = await AuthService.getProfile();
                localStorage.setItem('user', JSON.stringify(user));
                set({ user, isAuthenticated: true, isLoading: false });
            } catch {
                set({ user: null, isAuthenticated: false, isLoading: false });
            }
        } else {
            set({ user: null, isAuthenticated: false, isLoading: false });
        }
    },
}));
