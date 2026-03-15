using System.Text.Json;
using SmartLedger.Domain.Interfaces;

namespace SmartLedger.Infrastructure.FraudScoring;

public record FraudResult(int Score, Dictionary<string, object> Signals);


public class FraudScoringService(ITransactionRepository transactions)
{
    private const int FlagThreshold = 70;

    public async Task<FraudResult> ScoreAsync(
        Guid transactionId,
        Guid fromAccountId,
        decimal amount,
        CancellationToken ct = default)
    {
        var signals = new Dictionary<string, object>();
        int score = 0;

        // ── Rule 1: Velocity check ────────────────────────────────────────────
        // More than 5 transactions in the last 10 minutes is suspicious.
        var recentCount = await transactions.CountRecentByAccountAsync(
            fromAccountId, TimeSpan.FromMinutes(10), ct);

        if (recentCount > 5)
        {
            var points = Math.Min(30, recentCount * 5);
            score += points;
            signals["velocity"] = new { recentCount, points, window = "10min" };
        }

        // ── Rule 2: Amount anomaly ────────────────────────────────────────────
        // Transaction is more than 3× the account's 90-day average.
        var avg = await transactions.GetAverageAmountAsync(fromAccountId, 90, ct);
        if (avg > 0 && amount > avg * 3m)
        {
            var ratio = Math.Round(amount / avg, 2);
            var points = ratio > 10 ? 30 : 20;
            score += points;
            signals["amount_anomaly"] = new { amount, avg = Math.Round(avg, 2), ratio, points };
        }

        // ── Rule 3: New recipient ─────────────────────────────────────────────
        // First-ever transfer to this destination account.
        var tx = await transactions.GetByIdAsync(transactionId, ct);
        if (tx is not null)
        {
            var hasHistory = await transactions.HasPreviousTransferToAsync(
                fromAccountId, tx.ToAccountId, ct);

            if (!hasHistory)
            {
                score += 20;
                signals["new_recipient"] = new { toAccountId = tx.ToAccountId, points = 20 };
            }

            // ── Rule 4: Round-number bias ─────────────────────────────────────
            // Fraudulent transactions are statistically often round numbers.
            if (amount % 1000 == 0 || amount % 500 == 0)
            {
                score += 10;
                signals["round_number"] = new { amount, points = 10 };
            }
        }

        // ── Rule 5: Large single transfer ─────────────────────────────────────
        // Transactions over 50,000 in any currency get a base risk bump.
        if (amount > 50_000)
        {
            score += 15;
            signals["large_transfer"] = new { amount, points = 15 };
        }

        return new FraudResult(Math.Min(score, 100), signals);
    }

    public static string SerializeSignals(Dictionary<string, object> signals)
        => JsonSerializer.Serialize(signals, new JsonSerializerOptions
        {
            WriteIndented = false,
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
        });
}