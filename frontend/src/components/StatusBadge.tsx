import type { TransactionStatus } from '../types';

interface Props {
  status: TransactionStatus;
}

const config: Record<TransactionStatus, { label: string; classes: string }> = {
  Pending:   { label: 'Pending',   classes: 'bg-yellow-500/15 text-yellow-400 ring-yellow-500/30' },
  Complated: { label: 'Completed', classes: 'bg-green-500/15 text-green-400 ring-green-500/30' },
  Flagged:   { label: 'Flagged',   classes: 'bg-red-500/15 text-red-400 ring-red-500/30' },
  Rejected:  { label: 'Rejected',  classes: 'bg-slate-500/15 text-slate-400 ring-slate-500/30' },
};

export default function StatusBadge({ status }: Props) {
  const { label, classes } = config[status] ?? config['Pending'];
  return (
    <span className={`inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-semibold ring-1 ${classes}`}>
      {label}
    </span>
  );
}
