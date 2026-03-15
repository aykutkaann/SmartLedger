import { useQuery } from '@tanstack/react-query';
import { useParams, Link } from 'react-router-dom';
import { ArrowLeft, Copy } from 'lucide-react';
import toast from 'react-hot-toast';
import { transfersApi } from '../api/transfers';
import StatusBadge from '../components/StatusBadge';
import FraudScoreMeter from '../components/FraudScoreMeter';

function Field({ label, value }: { label: string; value: string }) {
  const copy = () => {
    navigator.clipboard.writeText(value);
    toast.success('Copied!');
  };
  return (
    <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-2 py-3 border-b border-white/5 last:border-0">
      <span className="text-slate-400 text-sm">{label}</span>
      <div className="flex items-center gap-2">
        <span className="text-white text-sm font-medium font-mono">{value}</span>
        <button onClick={copy} className="text-slate-500 hover:text-white transition">
          <Copy className="w-3.5 h-3.5" />
        </button>
      </div>
    </div>
  );
}

export default function TransferDetailPage() {
  const { id } = useParams<{ id: string }>();

  const { data: tx, isLoading, error } = useQuery({
    queryKey: ['transfer', id],
    queryFn: () => transfersApi.getDetail(id!),
    enabled: !!id,
  });

  if (isLoading) {
    return (
      <div className="space-y-4 animate-pulse max-w-2xl">
        <div className="h-8 bg-slate-800 rounded-xl w-48" />
        <div className="bg-slate-900 border border-white/10 rounded-2xl h-64" />
      </div>
    );
  }

  if (error || !tx) {
    return (
      <div className="text-center py-16">
        <p className="text-slate-400">Transaction not found or access denied.</p>
        <Link to="/transactions" className="mt-4 inline-block text-blue-400 hover:underline text-sm">
          ← Back to transactions
        </Link>
      </div>
    );
  }

  let fraudSignals: Record<string, unknown> = {};
  try {
    fraudSignals = JSON.parse(tx.fraudSignals);
  } catch {
    fraudSignals = {};
  }

  return (
    <div className="space-y-6 animate-fade-in max-w-2xl">
      {/* Back */}
      <Link
        to="/transactions"
        className="inline-flex items-center gap-1.5 text-slate-400 hover:text-white transition text-sm"
      >
        <ArrowLeft className="w-4 h-4" />
        Back to transactions
      </Link>

      {/* Header */}
      <div className="flex items-start justify-between gap-4">
        <div>
          <h1 className="text-2xl font-bold text-white">Transfer Details</h1>
          <p className="text-slate-400 text-sm mt-1 font-mono">{tx.id}</p>
        </div>
        <StatusBadge status={tx.status} />
      </div>

      {/* Amount card */}
      <div className="bg-gradient-to-br from-blue-900/30 to-slate-900 border border-blue-500/20 rounded-2xl p-6 text-center">
        <p className="text-slate-400 text-sm">Amount transferred</p>
        <p className="text-white text-4xl font-bold mt-2">
          {tx.amount.toLocaleString(undefined, { minimumFractionDigits: 2 })}{' '}
          <span className="text-2xl text-blue-400">{tx.currency}</span>
        </p>
        <p className="text-slate-500 text-sm mt-2">
          {new Date(tx.createdAt).toLocaleString()}
        </p>
      </div>

      {/* Details */}
      <div className="bg-slate-900 border border-white/10 rounded-2xl p-5">
        <h3 className="text-white font-semibold mb-4">Transaction Info</h3>
        <Field label="Transaction ID" value={tx.id} />
        <Field label="From Account" value={tx.fromAccountId} />
        <Field label="To Account" value={tx.toAccountId} />
        <Field label="Currency" value={tx.currency} />
        <Field label="Status" value={tx.status} />
        <Field
          label="Date"
          value={new Date(tx.createdAt).toLocaleString()}
        />
      </div>

      {/* Fraud analysis */}
      <div className="bg-slate-900 border border-white/10 rounded-2xl p-5">
        <h3 className="text-white font-semibold mb-4">Fraud Analysis</h3>
        <FraudScoreMeter score={tx.fraudScore} />

        {Object.keys(fraudSignals).length > 0 && (
          <div className="mt-4">
            <p className="text-slate-400 text-sm mb-2">Signals detected</p>
            <div className="space-y-2">
              {Object.entries(fraudSignals).map(([key, val]) => (
                <div
                  key={key}
                  className="flex items-center justify-between bg-white/5 rounded-xl px-3 py-2"
                >
                  <span className="text-slate-300 text-sm capitalize">{key.replace(/_/g, ' ')}</span>
                  <span className="text-slate-400 text-sm font-mono">{String(val)}</span>
                </div>
              ))}
            </div>
          </div>
        )}

        {Object.keys(fraudSignals).length === 0 && (
          <p className="text-slate-500 text-sm mt-3">
            {tx.fraudScore === 0
              ? 'Fraud scoring not yet processed.'
              : 'No specific signals identified.'}
          </p>
        )}
      </div>
    </div>
  );
}
