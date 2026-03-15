import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { Toaster } from 'react-hot-toast';

import Layout from './components/Layout';
import ProtectedRoute from './components/ProtectedRoute';

import LoginPage from './pages/LoginPage';
import RegisterPage from './pages/RegisterPage';
import DashboardPage from './pages/DashboardPage';
import AccountsPage from './pages/AccountsPage';
import TransferPage from './pages/TransferPage';
import TransactionsPage from './pages/TransactionsPage';
import TransferDetailPage from './pages/TransferDetailPage';
import FraudQueuePage from './pages/FraudQueuePage';

const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      staleTime: 30_000,
      retry: 1,
    },
  },
});

export default function App() {
  return (
    <QueryClientProvider client={queryClient}>
      <BrowserRouter>
        <Routes>
          {/* Public */}
          <Route path="/login" element={<LoginPage />} />
          <Route path="/register" element={<RegisterPage />} />
          <Route path="/" element={<Navigate to="/dashboard" replace />} />

          {/* Protected */}
          <Route
            element={
              <ProtectedRoute>
                <Layout />
              </ProtectedRoute>
            }
          >

            <Route path="/dashboard" element={<DashboardPage />} />
            <Route path="/accounts" element={<AccountsPage />} />
            <Route path="/transfer" element={<TransferPage />} />
            <Route path="/transactions" element={<TransactionsPage />} />
            <Route path="/transfers/:id" element={<TransferDetailPage />} />
            <Route path="/admin/fraud-queue" element={<FraudQueuePage />} />
          </Route>

          {/* 404 fallback */}
          <Route path="*" element={<Navigate to="/dashboard" replace />} />
        </Routes>
      </BrowserRouter>

      <Toaster
        position="top-right"
        toastOptions={{
          style: {
            background: '#1e293b',
            color: '#f1f5f9',
            border: '1px solid rgba(255,255,255,0.1)',
            borderRadius: '12px',
            fontSize: '14px',
          },
          success: { iconTheme: { primary: '#22c55e', secondary: '#fff' } },
          error: { iconTheme: { primary: '#ef4444', secondary: '#fff' } },
        }}
      />
    </QueryClientProvider>
  );
}
