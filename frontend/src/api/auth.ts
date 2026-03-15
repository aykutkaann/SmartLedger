import { apiClient } from './client';
import type { AuthResponse, LoginPayload, RegisterPayload } from '../types';

export const authApi = {
  register: (payload: RegisterPayload) =>
    apiClient.post<AuthResponse>('/auth/register', payload).then((r) => r.data),

  login: (payload: LoginPayload) =>
    apiClient.post<AuthResponse>('/auth/login', payload).then((r) => r.data),

  refresh: (refreshToken: string) =>
    apiClient.post<AuthResponse>('/auth/refresh', { refreshToken }).then((r) => r.data),
};
