import { useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import { ArrowLeftRight, ChevronLeft, ChevronRight, Search } from 'lucide-react';
import { accountsApi } from '../api/accounts';
import { transfersApi } from '../api/transfers';
import StatusBadge from '../components/StatusBadge';
import { Link } from 'react-router-dom';
import type { AccountDto, TransferDto } from '../types';

export default function TransactionsPage() {
  const [selectedAccountId, setSelectedAccountId] = useState<string>('');
  const [page, setPage] = useState(1);
  const [search, setSearch] = useState('');
  const PAGE_SIZE = 15;

  const { data: accounts = [], isLoading: loadingAccounts } = useQuery({
    queryKey: ['accounts'],
    queryFn: async () => {
      const list = await accountsApi.list();
      if (!selectedAccountId && list.length > 0) {
        setSelectedAccountId(list[0].id);
      }
      return list;
    },
  });

  const activeId = selectedAccountId || accounts[0]?.id;

  const { data: txPage, isLoading: loadingTx } = useQuery({
    queryKey: ['transactions', activeId, page],
    queryFn: () => transfersApi.getHistory(activeId!, page, PAGE_SIZE),
    enabled: !!activeId,
  });

  const filtered = (txPage?.items ?? []).filter((tx: TransferDto) =>
    search
      ? tx.id.toLowerCase().includes(search.toLowerCase()) ||
        tx.amount.toString().includes(search) ||
        tx.status.toLowerCase().includes(search.toLowerCase())
      : true,
  );

  return (
    <div className="space-y-6 animate-fade-in">
      {/* Header */}
      <div>
        <h1 className="text-2xl font-bold text-white">Transaction History</h1>
        <p className="text-slate-400 mt-1">View all movements on your accounts</p>
      </div>

      {/* Controls */}
      <div className="flex flex-col sm:flex-row gap-3">
        {/* Account selector */}
        {!loadingAccounts && accounts.length > 0 && (
          <select
            value={activeId ?? ''}
            onChange={(e) => {
              setSelectedAccountId(e.target.value);
              setPage(1);
            }}
            className="bg-slate-800 border border-white/10 text-white rounded-xl px-4 py-2.5 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500 transition min-w-0 sm:w-64"
          >
            {accounts.map((account: AccountDto) => (
              <option key={account.id} value={account.id}>
                {account.currency} — {account.iban.slice(-8)}
              </option>
            ))}
          </select>
        )}

        {/* Search */}
        <div className="relative flex-1">
          <Search className="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-slate-400" />
          <input
            type="text"
            value={search}
            onChange={(e) => setSearch(e.target.value)}
            placeholder="Search by ID, amount, or status…"
            className="w-full bg-slate-800 border border-white/10 text-white placeholder-slate-500 rounded-xl pl-10 pr-4 py-2.5 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500 transition"
          />
        </div>
      </div>

      {/* Table */}
      <div className="bg-slate-900 border border-white/10 rounded-2xl overflow-hidden">
        {/* Table header */}
        <div className="grid grid-cols-5 gap-4 px-5 py-3 border-b border-white/10 text-xs font-medium text-slate-500 uppercase tracking-wider">
          <span className="col-span-2">Transaction ID</span>
          <span className="text-right">Amount</span>
          <span className="text-center">Status</span>
          <span className="text-right">Date</span>
        </div>

        {loadingTx ? (
          <div className="divide-y divide-white/5">
            {Array.from({ length: 5 }).map((_, i) => (
              <div key={i} className="px-5 py-4 animate-pulse flex gap-4">
                <div className="flex-1 h-4 bg-white/5 rounded" />
                <div className="w-24 h-4 bg-white/5 rounded" />
                <div className="w-20 h-4 bg-white/5 rounded" />
                <div className="w-24 h-4 bg-white/5 rounded" />
              </div>
            ))}
          </div>
        ) : !activeId ? (
          <div className="p-12 text-center">
            <p className="text-slate-400">No accounts found.</p>
          </div>
        ) : filtered.length === 0 ? (
          <div className="p-12 text-center">
            <ArrowLeftRight className="w-10 h-10 text-slate-700 mx-auto mb-3" />
            <p className="text-slate-400">No transactions found.</p>
          </div>
        ) : (
          <div className="divide-y divide-white/5">
            {filtered.map((tx: TransferDto) => {
              const isOutgoing = tx.fromAccountId === activeId;
              return (
                <Link
                  key={tx.id}
                  to={`/transfers/${tx.id}`}
                  className="grid grid-cols-5 gap-4 items-center px-5 py-3.5 hover:bg-white/3 transition group"
                >
                  <div className="col-span-2 flex items-center gap-3 min-w-0">
                    <div
                      className={`w-8 h-8 rounded-full flex items-center justify-center flex-shrink-0 ${
                        isOutgoing ? 'bg-red-500/10' : 'bg-green-500/10'
                      }`}
                    >
                      <ArrowLeftRight
                        className={`w-4 h-4 ${isOutgoing ? 'text-red-400' : 'text-green-400'}`}
                      />
                    </div>
                    <span className="text-slate-300 text-sm font-mono truncate">
                      {tx.id.slice(0, 8)}…
                    </span>
                  </div>
                  <span
                    className={`text-sm font-semibold text-right ${
                      isOutgoing ? 'text-red-400' : 'text-green-400'
                    }`}
                  >
                    {isOutgoing ? '−' : '+'}
                    {tx.amount.toLocaleString(undefined, { minimumFractionDigits: 2 })}{' '}
                    {tx.currency}
                  </span>
                  <div className="flex justify-center">
                    <StatusBadge status={tx.status} />
                  </div>
                  <span className="text-slate-500 text-xs text-right">
                    {new Date(tx.createdAt).toLocaleDateString()}
                  </span>
                </Link>
              );
            })}
          </div>
        )}

        {/* Pagination */}
        {txPage && (
          <div className="flex items-center justify-between px-5 py-3 border-t border-white/10">
            <span className="text-slate-500 text-sm">
              Page {page}
            </span>
            <div className="flex gap-2">
              <button
                onClick={() => setPage((p) => Math.max(1, p - 1))}
                disabled={page <= 1}
                className="p-1.5 rounded-lg bg-white/5 text-slate-400 hover:bg-white/10 disabled:opacity-30 disabled:cursor-not-allowed transition"
              >
                <ChevronLeft className="w-4 h-4" />
              </button>
              <button
                onClick={() => setPage((p) => p + 1)}
                disabled={(txPage.items.length ?? 0) < PAGE_SIZE}
                className="p-1.5 rounded-lg bg-white/5 text-slate-400 hover:bg-white/10 disabled:opacity-30 disabled:cursor-not-allowed transition"
              >
                <ChevronRight className="w-4 h-4" />
              </button>
            </div>
          </div>
        )}
      </div>
    </div>
  );
}
