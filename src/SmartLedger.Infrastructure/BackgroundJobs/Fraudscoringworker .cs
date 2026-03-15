using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SmartLedger.Domain.Interfaces;
using SmartLedger.Infrastructure.FraudScoring;
using SmartLedger.Infrastructure.Persistence;

namespace SmartLedger.Infrastructure.BackgroundJobs;


public class FraudScoringWorker(
    IServiceScopeFactory scopeFactory,
    ILogger<FraudScoringWorker> logger) : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromSeconds(10);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Fraud scoring worker started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessBatchAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Error during fraud scoring batch.");
            }

            await Task.Delay(Interval, stoppingToken);
        }

        logger.LogInformation("Fraud scoring worker stopped.");
    }

    private async Task ProcessBatchAsync(CancellationToken ct)
    {
        // Use a fresh DI scope per batch so DbContext is short-lived
        await using var scope = scopeFactory.CreateAsyncScope();

        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var txRepo = scope.ServiceProvider.GetRequiredService<ITransactionRepository>();
        var scorer = new FraudScoringService(txRepo);

        var pending = (await txRepo.GetPendingScoringAsync(ct)).ToList();

        if (pending.Count == 0) return;

        logger.LogInformation("Scoring {Count} pending transaction(s).", pending.Count);

        foreach (var tx in pending)
        {
            try
            {
                var result = await scorer.ScoreAsync(tx.Id, tx.FromAccountId, tx.Amount, ct);
                var json = FraudScoringService.SerializeSignals(result.Signals);

                tx.SetFraudResult(result.Score, json);

                logger.LogInformation(
                    "Transaction {Id} scored {Score} — status → {Status}",
                    tx.Id, result.Score, tx.Status);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to score transaction {Id}.", tx.Id);
            }
        }

        await db.SaveChangesAsync(ct);
    }
}