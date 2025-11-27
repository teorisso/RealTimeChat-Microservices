import axios from 'axios';
import type { AuthResponse } from '../types';

// Create instances for each microservice
export const authApi = axios.create({
    baseURL: 'http://localhost:5257/api',
});

export const groupsApi = axios.create({
    baseURL: 'http://localhost:5167/api',
});

export const messagesApi = axios.create({
    baseURL: 'http://localhost:5266/api',
});

// Helper to attach interceptor to an instance
const attachInterceptor = (axiosInstance: any) => {
    axiosInstance.interceptors.request.use((config: any) => {
        const token = localStorage.getItem('accessToken');
        if (token) {
            config.headers.Authorization = `Bearer ${token}`;
        }
        return config;
    });

    axiosInstance.interceptors.response.use(
        (response: any) => response,
        async (error: any) => {
            const originalRequest = error.config;
            if (error.response?.status === 401 && !originalRequest._retry) {
                originalRequest._retry = true;
                try {
                    const refreshToken = localStorage.getItem('refreshToken');
                    const userStr = localStorage.getItem('user');
                    const user = userStr ? JSON.parse(userStr) : null;

                    if (!refreshToken || !user?.email) throw new Error('No refresh token or user email');

                    // Always use authApi for refresh
                    const response = await axios.post<AuthResponse>('http://localhost:5257/api/auth/refresh', {
                        email: user.email,
                        token: refreshToken,
                    });

                    if (response.data.success && response.data.data) {
                        const { accessToken, refreshToken: newRefreshToken } = response.data.data;
                        localStorage.setItem('accessToken', accessToken);
                        localStorage.setItem('refreshToken', newRefreshToken);

                        originalRequest.headers.Authorization = `Bearer ${accessToken}`;
                        return axiosInstance(originalRequest);
                    }
                } catch (refreshError) {
                    localStorage.removeItem('accessToken');
                    localStorage.removeItem('refreshToken');
                    localStorage.removeItem('user');
                    window.location.href = '/login';
                    return Promise.reject(refreshError);
                }
            }
            return Promise.reject(error);
        }
    );
};

// Attach interceptors to all instances
attachInterceptor(authApi);
attachInterceptor(groupsApi);
attachInterceptor(messagesApi);

// Default export for backward compatibility if needed, but prefer named exports
export default authApi;
