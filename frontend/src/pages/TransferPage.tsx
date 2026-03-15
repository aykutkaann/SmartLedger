import { useState } from 'react';
import { useQuery, useMutation } from '@tanstack/react-query';
import { ArrowLeftRight, Send } from 'lucide-react';
import toast from 'react-hot-toast';
import { accountsApi } from '../api/accounts';
import { transfersApi } from '../api/transfers';
import { useNavigate } from 'react-router-dom';
import type { AccountDto } from '../types';

export default function TransferPage() {
  const navigate = useNavigate();
  const [form, setForm] = useState({
    fromAccountId: '',
    toAccountId: '',
    amount: '',
    description: '',
  });

  const { data: accounts = [], isLoading } = useQuery({
    queryKey: ['accounts'],
    queryFn: accountsApi.list,
  });

  const activeAccounts = accounts.filter((a: AccountDto) => a.status === 'Active');

  const mutation = useMutation({
    mutationFn: () =>
      transfersApi.initiate({
        fromAccountId: form.fromAccountId,
        toAccountId: form.toAccountId,
        amount: parseFloat(form.amount),
        description: form.description || undefined,
      }),
    onSuccess: (data) => {
      toast.success('Transfer initiated successfully!');
      navigate(`/transfers/${data.id}`);
    },
    onError: (err: unknown) => {
      const msg =
        (err as { response?: { data?: { detail?: string; title?: string } } })?.response?.data
          ?.detail ??
        (err as { response?: { data?: { detail?: string; title?: string } } })?.response?.data
          ?.title ??
        'Transfer failed. Please check your details.';
      toast.error(msg);
    },
  });

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    if (form.fromAccountId === form.toAccountId) {
      toast.error('Source and destination accounts must be different.');
      return;
    }
    if (parseFloat(form.amount) <= 0) {
      toast.error('Amount must be greater than zero.');
      return;
    }
    mutation.mutate();
  };

  const fromAccount = activeAccounts.find((a: AccountDto) => a.id === form.fromAccountId);

  return (
    <div className="space-y-6 animate-fade-in max-w-2xl">
      {/* Header */}
      <div>
        <h1 className="text-2xl font-bold text-white">New Transfer</h1>
        <p className="text-slate-400 mt-1">Send money between accounts</p>
      </div>

      <div className="bg-slate-900 border border-white/10 rounded-2xl p-6">
        <form onSubmit={handleSubmit} className="space-y-5">
          {/* From account */}
          <div>
            <label className="block text-sm font-medium text-slate-300 mb-1.5">From Account</label>
            {isLoading ? (
              <div className="h-12 bg-slate-800 rounded-xl animate-pulse" />
            ) : (
              <select
                required
                value={form.fromAccountId}
                onChange={(e) => setForm({ ...form, fromAccountId: e.target.value })}
                className="w-full bg-slate-800 border border-white/10 text-white rounded-xl px-4 py-3 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500 transition"
              >
                <option value="" disabled>
                  Select source account
                </option>
                {activeAccounts.map((account: AccountDto) => (
                  <option key={account.id} value={account.id}>
                    {account.currency} — {account.iban.slice(-8)} (
                    {account.balance.toLocaleString(undefined, { minimumFractionDigits: 2 })}{' '}
                    {account.currency})
                  </option>
                ))}
              </select>
            )}
          </div>

          {/* Arrow divider */}
          <div className="flex items-center gap-3">
            <div className="flex-1 h-px bg-white/10" />
            <div className="w-8 h-8 rounded-full bg-blue-600/20 flex items-center justify-center">
              <ArrowLeftRight className="w-4 h-4 text-blue-400" />
            </div>
            <div className="flex-1 h-px bg-white/10" />
          </div>

          {/* To account */}
          <div>
            <label className="block text-sm font-medium text-slate-300 mb-1.5">To Account</label>
            {isLoading ? (
              <div className="h-12 bg-slate-800 rounded-xl animate-pulse" />
            ) : (
              <select
                required
                value={form.toAccountId}
                onChange={(e) => setForm({ ...form, toAccountId: e.target.value })}
                className="w-full bg-slate-800 border border-white/10 text-white rounded-xl px-4 py-3 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500 transition"
              >
                <option value="" disabled>
                  Select destination account
                </option>
                {activeAccounts
                  .filter((a: AccountDto) => a.id !== form.fromAccountId)
                  .map((account: AccountDto) => (
                    <option key={account.id} value={account.id}>
                      {account.currency} — {account.iban.slice(-8)} (
                      {account.balance.toLocaleString(undefined, { minimumFractionDigits: 2 })}{' '}
                      {account.currency})
                    </option>
                  ))}
              </select>
            )}
          </div>

          {/* Amount */}
          <div>
            <label className="block text-sm font-medium text-slate-300 mb-1.5">Amount</label>
            <div className="relative">
              <span className="absolute left-4 top-1/2 -translate-y-1/2 text-slate-400 font-medium text-sm">
                {fromAccount?.currency ?? '$'}
              </span>
              <input
                type="number"
                required
                min="0.01"
                step="0.01"
                value={form.amount}
                onChange={(e) => setForm({ ...form, amount: e.target.value })}
                placeholder="0.00"
                className="w-full bg-slate-800 border border-white/10 text-white placeholder-slate-500 rounded-xl pl-10 pr-4 py-3 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500 transition"
              />
            </div>
            {fromAccount && (
              <p className="text-slate-500 text-xs mt-1">
                Available:{' '}
                {fromAccount.balance.toLocaleString(undefined, { minimumFractionDigits: 2 })}{' '}
                {fromAccount.currency}
              </p>
            )}
          </div>

          {/* Description */}
          <div>
            <label className="block text-sm font-medium text-slate-300 mb-1.5">
              Description{' '}
              <span className="text-slate-500 font-normal">(optional)</span>
            </label>
            <input
              type="text"
              value={form.description}
              onChange={(e) => setForm({ ...form, description: e.target.value })}
              placeholder="e.g. Rent payment"
              maxLength={200}
              className="w-full bg-slate-800 border border-white/10 text-white placeholder-slate-500 rounded-xl px-4 py-3 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500 transition"
            />
          </div>

          <button
            type="submit"
            disabled={mutation.isPending || !form.fromAccountId || !form.toAccountId || !form.amount}
            className="w-full flex items-center justify-center gap-2 bg-blue-600 hover:bg-blue-500 disabled:bg-blue-800 disabled:cursor-not-allowed text-white font-semibold py-3 rounded-xl transition shadow-lg shadow-blue-600/20 text-sm"
          >
            {mutation.isPending ? (
              <>
                <svg className="animate-spin w-4 h-4" viewBox="0 0 24 24" fill="none">
                  <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4" />
                  <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8v8z" />
                </svg>
                Processing…
              </>
            ) : (
              <>
                <Send className="w-4 h-4" />
                Send Transfer
              </>
            )}
          </button>
        </form>
      </div>
    </div>
  );
}
