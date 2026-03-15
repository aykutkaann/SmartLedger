import { ShieldAlert } from 'lucide-react';

export default function FraudQueuePage() {
  return (
    <div className="space-y-6 animate-fade-in">
      <div>
        <h1 className="text-2xl font-bold text-white">Fraud Queue</h1>
        <p className="text-slate-400 mt-1">Admin — flagged transactions awaiting review</p>
      </div>

      <div className="bg-slate-900 border border-white/10 rounded-2xl p-12 text-center">
        <ShieldAlert className="w-12 h-12 text-orange-400 mx-auto mb-4" />
        <p className="text-white font-semibold">Fraud queue coming soon</p>
        <p className="text-slate-400 text-sm mt-2">
          The admin fraud review queue endpoint is pending implementation on the backend.
        </p>
      </div>
    </div>
  );
}
