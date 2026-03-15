/// <reference types="vite/client" />
import axios from 'axios';
import { useAuthStore } from '../store/authStore';

const API_BASE = import.meta.env.VITE_API_URL ?? '';

export const apiClient = axios.create({
  baseURL: API_BASE,
  headers: { 'Content-Type': 'application/json' },
  withCredentials: false,
});

// ── Attach JWT on every request ───────────────────────────────────────────────
apiClient.interceptors.request.use((config) => {
  const token = useAuthStore.getState().accessToken;
  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});

// ── Auto-refresh on 401 ───────────────────────────────────────────────────────
let isRefreshing = false;
let failQueue: Array<{
  resolve: (value: string) => void;
  reject: (reason: unknown) => void;
}> = [];

function processQueue(error: unknown, token: string | null) {
  failQueue.forEach((p) => (token ? p.resolve(token) : p.reject(error)));
  failQueue = [];
}

apiClient.interceptors.response.use(
  (res) => res,
  async (error) => {
    const original = error.config;
    if (error.response?.status === 401 && !original._retry) {
      if (isRefreshing) {
        return new Promise<string>((resolve, reject) => {
          failQueue.push({ resolve, reject });
        }).then((token) => {
          original.headers.Authorization = `Bearer ${token}`;
          return apiClient(original);
        });
      }
      original._retry = true;
      isRefreshing = true;

      const { refreshToken, setTokens, logout } = useAuthStore.getState();

      if (!refreshToken) {
        logout();
        return Promise.reject(error);
      }

      try {
        const { data } = await axios.post(`${API_BASE}/auth/refresh`, {
          refreshToken,
        });
        setTokens(data.accessToken, data.refreshToken);
        original.headers.Authorization = `Bearer ${data.accessToken}`;
        processQueue(null, data.accessToken);
        return apiClient(original);
      } catch (refreshError) {
        processQueue(refreshError, null);
        logout();
        return Promise.reject(refreshError);
      } finally {
        isRefreshing = false;
      }
    }
    return Promise.reject(error);
  },
);
