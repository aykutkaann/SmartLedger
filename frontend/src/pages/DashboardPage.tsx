import { useQuery } from '@tanstack/react-query';
import { CreditCard, ArrowLeftRight, TrendingUp, TrendingDown, AlertTriangle } from 'lucide-react';
import { accountsApi } from '../api/accounts';
import { transfersApi } from '../api/transfers';
import { useAuthStore } from '../store/authStore';
import { Link } from 'react-router-dom';
import StatusBadge from '../components/StatusBadge';

function StatCard({
  label,
  value,
  sub,
  icon,
  color,
}: {
  label: string;
  value: string;
  sub?: string;
  icon: React.ReactNode;
  color: string;
}) {
  return (
    <div className="bg-slate-900 border border-white/10 rounded-2xl p-5 flex items-start gap-4 animate-slide-up">
      <div className={`p-2.5 rounded-xl ${color} flex-shrink-0`}>{icon}</div>
      <div className="min-w-0">
        <p className="text-slate-400 text-sm">{label}</p>
        <p className="text-white text-2xl font-bold mt-0.5 truncate">{value}</p>
        {sub && <p className="text-slate-500 text-xs mt-1">{sub}</p>}
      </div>
    </div>
  );
}

export default function DashboardPage() {
  const email = useAuthStore((s) => s.email);

  const { data: accounts = [], isLoading: loadingAccounts } = useQuery({
    queryKey: ['accounts'],
    queryFn: accountsApi.list,
  });

  const firstAccountId = accounts[0]?.id;
  const { data: txPage, isLoading: loadingTx } = useQuery({
    queryKey: ['transactions', firstAccountId],
    queryFn: () => transfersApi.getHistory(firstAccountId!, 1, 5),
    enabled: !!firstAccountId,
  });

  const totalBalance = accounts
    .filter((a) => a.status === 'Active')
    .reduce((sum, a) => sum + a.balance, 0);

  const activeAccounts = accounts.filter((a) => a.status === 'Active').length;
  const flaggedTx = txPage?.items.filter((t) => t.status === 'Flagged').length ?? 0;

  const firstName = email?.split('@')[0] ?? 'User';

  return (
    <div className="space-y-8 animate-fade-in">
      {/* Header */}
      <div>
        <h1 className="text-2xl font-bold text-white">
          Good morning, <span className="text-blue-400 capitalize">{firstName}</span> 👋
        </h1>
        <p className="text-slate-400 mt-1">Here's your financial overview</p>
      </div>

      {/* Stats */}
      <div className="grid grid-cols-1 sm:grid-cols-2 xl:grid-cols-4 gap-4">
        <StatCard
          label="Total Balance"
          value={loadingAccounts ? '…' : `$${totalBalance.toLocaleString(undefined, { minimumFractionDigits: 2 })}`}
          sub="Across all active accounts"
          icon={<TrendingUp className="w-5 h-5 text-blue-400" />}
          color="bg-blue-500/10"
        />
        <StatCard
          label="Active Accounts"
          value={loadingAccounts ? '…' : String(activeAccounts)}
          sub={`${accounts.length} total`}
          icon={<CreditCard className="w-5 h-5 text-emerald-400" />}
          color="bg-emerald-500/10"
        />
        <StatCard
          label="Recent Transactions"
          value={loadingTx ? '…' : String(txPage?.items.length ?? 0)}
          sub="Last 5 on primary account"
          icon={<ArrowLeftRight className="w-5 h-5 text-purple-400" />}
          color="bg-purple-500/10"
        />
        <StatCard
          label="Flagged"
          value={loadingTx ? '…' : String(flaggedTx)}
          sub="Suspicious transactions"
          icon={<AlertTriangle className="w-5 h-5 text-orange-400" />}
          color="bg-orange-500/10"
        />
      </div>

      {/* Accounts overview */}
      <section>
        <div className="flex items-center justify-between mb-4">
          <h2 className="text-lg font-semibold text-white">Your Accounts</h2>
          <Link to="/accounts" className="text-sm text-blue-400 hover:text-blue-300 transition">
            View all →
          </Link>
        </div>

        {loadingAccounts ? (
          <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-4">
            {[1, 2].map((i) => (
              <div key={i} className="bg-slate-900 border border-white/10 rounded-2xl p-5 animate-pulse h-36" />
            ))}
          </div>
        ) : accounts.length === 0 ? (
          <div className="bg-slate-900 border border-white/10 rounded-2xl p-8 text-center">
            <CreditCard className="w-10 h-10 text-slate-600 mx-auto mb-3" />
            <p className="text-slate-400">No accounts yet.</p>
            <Link to="/accounts" className="mt-3 inline-block text-blue-400 text-sm hover:underline">
              Open your first account
            </Link>
          </div>
        ) : (
          <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-4">
            {accounts.slice(0, 3).map((account) => (
              <div
                key={account.id}
                className="bg-gradient-to-br from-slate-800 to-slate-900 border border-white/10 rounded-2xl p-5 space-y-4 hover:border-blue-500/30 transition"
              >
                <div className="flex justify-between items-start">
                  <div>
                    <p className="text-slate-400 text-xs font-medium uppercase tracking-wider">
                      {account.currency} Account
                    </p>
                    <p className="text-white font-bold text-xl mt-1">
                      {account.balance.toLocaleString(undefined, { minimumFractionDigits: 2 })}{' '}
                      <span className="text-sm font-normal text-slate-400">{account.currency}</span>
                    </p>
                  </div>
                  <span
                    className={`text-xs px-2 py-1 rounded-lg font-medium ${
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
                  <p className="text-slate-500 text-xs">IBAN</p>
                  <p className="text-slate-300 text-xs font-mono mt-0.5 break-all">{account.iban}</p>
                </div>
              </div>
            ))}
          </div>
        )}
      </section>

      {/* Recent transactions */}
      <section>
        <div className="flex items-center justify-between mb-4">
          <h2 className="text-lg font-semibold text-white">Recent Transactions</h2>
          <Link to="/transactions" className="text-sm text-blue-400 hover:text-blue-300 transition">
            View all →
          </Link>
        </div>

        <div className="bg-slate-900 border border-white/10 rounded-2xl overflow-hidden">
          {loadingTx ? (
            <div className="p-8 text-center text-slate-400">Loading…</div>
          ) : !firstAccountId ? (
            <div className="p-8 text-center">
              <p className="text-slate-400 text-sm">Open an account to see transactions.</p>
            </div>
          ) : (txPage?.items.length ?? 0) === 0 ? (
            <div className="p-8 text-center">
              <TrendingDown className="w-10 h-10 text-slate-600 mx-auto mb-3" />
              <p className="text-slate-400 text-sm">No transactions yet.</p>
            </div>
          ) : (
            <div className="divide-y divide-white/5">
              {txPage!.items.map((tx) => (
                <div key={tx.id} className="flex items-center justify-between px-5 py-3.5 hover:bg-white/3 transition">
                  <div className="flex items-center gap-3 min-w-0">
                    <div className="w-8 h-8 rounded-full bg-white/5 flex items-center justify-center flex-shrink-0">
                      <ArrowLeftRight className="w-4 h-4 text-slate-400" />
                    </div>
                    <div className="min-w-0">
                      <p className="text-white text-sm font-medium truncate">Transfer</p>
                      <p className="text-slate-500 text-xs">
                        {new Date(tx.createdAt).toLocaleDateString()}
                      </p>
                    </div>
                  </div>
                  <div className="flex items-center gap-3 flex-shrink-0">
                    <span className="text-white font-semibold text-sm">
                      {tx.amount.toLocaleString(undefined, { minimumFractionDigits: 2 })} {tx.currency}
                    </span>
                    <StatusBadge status={tx.status} />
                  </div>
                </div>
              ))}
            </div>
          )}
        </div>
      </section>
    </div>
  );
}
