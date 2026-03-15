import { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { Plus, CreditCard, Trash2, X, Check } from 'lucide-react';
import toast from 'react-hot-toast';
import { accountsApi } from '../api/accounts';
import type { AccountDto } from '../types';

const SUPPORTED_CURRENCIES = ['USD', 'EUR', 'GBP', 'TRY', 'CHF', 'JPY', 'CAD', 'AUD'];

function AccountCard({
  account,
  onClose,
}: {
  account: AccountDto;
  onClose: (id: string) => void;
}) {
  const [confirmClose, setConfirmClose] = useState(false);

  const currencyColors: Record<string, string> = {
    USD: 'from-green-900/40 to-slate-900',
    EUR: 'from-blue-900/40 to-slate-900',
    GBP: 'from-purple-900/40 to-slate-900',
    TRY: 'from-red-900/40 to-slate-900',
    CHF: 'from-orange-900/40 to-slate-900',
    JPY: 'from-pink-900/40 to-slate-900',
    CAD: 'from-amber-900/40 to-slate-900',
    AUD: 'from-teal-900/40 to-slate-900',
  };

  const gradient = currencyColors[account.currency] ?? 'from-slate-800 to-slate-900';

  return (
    <div
      className={`bg-gradient-to-br ${gradient} border border-white/10 rounded-2xl p-5 space-y-4 transition hover:border-white/20 animate-slide-up`}
    >
      <div className="flex justify-between items-start">
        <div className="flex items-center gap-2">
          <CreditCard className="w-5 h-5 text-slate-400" />
          <span className="text-slate-300 text-sm font-medium">{account.currency} Account</span>
        </div>
        <span
          className={`text-xs px-2.5 py-1 rounded-lg font-semibold ${
            account.status === 'Active'
              ? 'bg-green-500/15 text-green-400'
              : account.status === 'Frozen'
              ? 'bg-blue-500/15 text-blue-400'
              : 'bg-slate-500/15 text-slate-400'
          }`}
        >
          {account.status}
        </span>
      </div>

      <div>
        <p className="text-slate-500 text-xs mb-1">Balance</p>
        <p className="text-white font-bold text-3xl">
          {account.balance.toLocaleString(undefined, { minimumFractionDigits: 2 })}
          <span className="text-lg font-normal text-slate-400 ml-1">{account.currency}</span>
        </p>
      </div>

      <div className="bg-white/5 rounded-xl p-3">
        <p className="text-slate-500 text-xs mb-1">IBAN</p>
        <p className="text-slate-200 font-mono text-sm break-all">{account.iban}</p>
      </div>

      <div className="flex justify-between items-center text-xs text-slate-500">
        <span>Opened {new Date(account.createdAt).toLocaleDateString()}</span>
        {account.status !== 'Closed' && (
          <div className="flex items-center gap-2">
            {confirmClose ? (
              <>
                <span className="text-red-400">Confirm close?</span>
                <button
                  onClick={() => onClose(account.id)}
                  className="p-1 rounded-lg bg-red-500/15 text-red-400 hover:bg-red-500/25 transition"
                >
                  <Check className="w-3.5 h-3.5" />
                </button>
                <button
                  onClick={() => setConfirmClose(false)}
                  className="p-1 rounded-lg bg-white/5 text-slate-400 hover:bg-white/10 transition"
                >
                  <X className="w-3.5 h-3.5" />
                </button>
              </>
            ) : (
              <button
                onClick={() => setConfirmClose(true)}
                className="flex items-center gap-1 text-slate-500 hover:text-red-400 transition"
              >
                <Trash2 className="w-3.5 h-3.5" />
                Close
              </button>
            )}
          </div>
        )}
      </div>
    </div>
  );
}

export default function AccountsPage() {
  const qc = useQueryClient();
  const [showModal, setShowModal] = useState(false);
  const [currency, setCurrency] = useState('USD');

  const { data: accounts = [], isLoading } = useQuery({
    queryKey: ['accounts'],
    queryFn: accountsApi.list,
  });

  const createMutation = useMutation({
    mutationFn: () => accountsApi.create(currency),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['accounts'] });
      toast.success('Account opened!');
      setShowModal(false);
    },
    onError: () => toast.error('Failed to create account'),
  });

  const closeMutation = useMutation({
    mutationFn: (id: string) => accountsApi.close(id),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['accounts'] });
      toast.success('Account closed');
    },
    onError: () => toast.error('Could not close account. Make sure balance is zero.'),
  });

  return (
    <div className="space-y-6 animate-fade-in">
      {/* Header */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
        <div>
          <h1 className="text-2xl font-bold text-white">Accounts</h1>
          <p className="text-slate-400 mt-1">Manage your bank accounts</p>
        </div>
        <button
          onClick={() => setShowModal(true)}
          className="flex items-center gap-2 bg-blue-600 hover:bg-blue-500 text-white font-semibold px-4 py-2.5 rounded-xl transition shadow-lg shadow-blue-600/20 text-sm self-start sm:self-auto"
        >
          <Plus className="w-4 h-4" />
          Open Account
        </button>
      </div>

      {/* Accounts grid */}
      {isLoading ? (
        <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-4">
          {[1, 2, 3].map((i) => (
            <div key={i} className="bg-slate-900 border border-white/10 rounded-2xl h-52 animate-pulse" />
          ))}
        </div>
      ) : accounts.length === 0 ? (
        <div className="bg-slate-900 border border-white/10 rounded-2xl p-12 text-center">
          <CreditCard className="w-12 h-12 text-slate-700 mx-auto mb-4" />
          <p className="text-white font-semibold">No accounts yet</p>
          <p className="text-slate-400 text-sm mt-1">Open your first account to get started.</p>
          <button
            onClick={() => setShowModal(true)}
            className="mt-5 inline-flex items-center gap-2 bg-blue-600 hover:bg-blue-500 text-white font-semibold px-4 py-2 rounded-xl text-sm transition"
          >
            <Plus className="w-4 h-4" /> Open Account
          </button>
        </div>
      ) : (
        <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-4">
          {accounts.map((account) => (
            <AccountCard key={account.id} account={account} onClose={(id) => closeMutation.mutate(id)} />
          ))}
        </div>
      )}

      {/* Create account modal */}
      {showModal && (
        <div className="fixed inset-0 bg-black/70 backdrop-blur-sm z-50 flex items-center justify-center p-4">
          <div className="bg-slate-900 border border-white/10 rounded-2xl p-6 w-full max-w-sm shadow-2xl animate-slide-up">
            <div className="flex items-center justify-between mb-5">
              <h3 className="text-white font-bold text-lg">Open New Account</h3>
              <button
                onClick={() => setShowModal(false)}
                className="text-slate-400 hover:text-white transition"
              >
                <X className="w-5 h-5" />
              </button>
            </div>

            <div className="space-y-4">
              <div>
                <label className="block text-sm font-medium text-slate-300 mb-1.5">Currency</label>
                <select
                  value={currency}
                  onChange={(e) => setCurrency(e.target.value)}
                  className="w-full bg-slate-800 border border-white/10 text-white rounded-xl px-4 py-3 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500 transition"
                >
                  {SUPPORTED_CURRENCIES.map((c) => (
                    <option key={c} value={c}>{c}</option>
                  ))}
                </select>
              </div>

              <div className="flex gap-3 pt-2">
                <button
                  onClick={() => setShowModal(false)}
                  className="flex-1 bg-white/5 hover:bg-white/10 text-slate-300 font-medium py-2.5 rounded-xl text-sm transition"
                >
                  Cancel
                </button>
                <button
                  onClick={() => createMutation.mutate()}
                  disabled={createMutation.isPending}
                  className="flex-1 bg-blue-600 hover:bg-blue-500 disabled:bg-blue-800 text-white font-semibold py-2.5 rounded-xl text-sm transition"
                >
                  {createMutation.isPending ? 'Opening…' : 'Open Account'}
                </button>
              </div>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
