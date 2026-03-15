interface Props {
  score: number;
}

export default function FraudScoreMeter({ score }: Props) {
  const color =
    score >= 70
      ? 'bg-red-500'
      : score >= 40
      ? 'bg-yellow-500'
      : 'bg-green-500';

  const label =
    score >= 70 ? 'High Risk' : score >= 40 ? 'Medium Risk' : 'Low Risk';

  const textColor =
    score >= 70 ? 'text-red-400' : score >= 40 ? 'text-yellow-400' : 'text-green-400';

  return (
    <div className="space-y-1.5">
      <div className="flex justify-between items-center">
        <span className="text-xs text-slate-400">Fraud Score</span>
        <span className={`text-xs font-semibold ${textColor}`}>
          {score} — {label}
        </span>
      </div>
      <div className="w-full h-2 bg-white/10 rounded-full overflow-hidden">
        <div
          className={`h-full rounded-full transition-all duration-700 ${color}`}
          style={{ width: `${score}%` }}
        />
      </div>
    </div>
  );
}
