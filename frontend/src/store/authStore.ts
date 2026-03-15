import { create } from 'zustand';
import { persist } from 'zustand/middleware';

interface AuthState {
  accessToken: string | null;
  refreshToken: string | null;
  email: string | null;
  role: string | null;
  isAuthenticated: boolean;

  setAuth: (accessToken: string, refreshToken: string, email: string, role: string) => void;
  setTokens: (accessToken: string, refreshToken: string) => void;
  logout: () => void;
}

export const useAuthStore = create<AuthState>()(
  persist(
    (set) => ({
      accessToken: null,
      refreshToken: null,
      email: null,
      role: null,
      isAuthenticated: false,

      setAuth: (accessToken, refreshToken, email, role) =>
        set({ accessToken, refreshToken, email, role, isAuthenticated: true }),

      setTokens: (accessToken, refreshToken) =>
        set({ accessToken, refreshToken }),

      logout: () =>
        set({
          accessToken: null,
          refreshToken: null,
          email: null,
          role: null,
          isAuthenticated: false,
        }),
    }),
    {
      name: 'smartledger-auth',
      partialize: (state) => ({
        accessToken: state.accessToken,
        refreshToken: state.refreshToken,
        email: state.email,
        role: state.role,
        isAuthenticated: state.isAuthenticated,
      }),
    },
  ),
);
