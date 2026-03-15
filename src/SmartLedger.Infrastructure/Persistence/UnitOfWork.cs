using Microsoft.EntityFrameworkCore;
using SmartLedger.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartLedger.Infrastructure.Persistence
{
    public class UnitOfWork(AppDbContext db) : IUnitOfWork
    {

        public Task<int> SaveChangesAsync(CancellationToken ct = default) => db.SaveChangesAsync(ct);


        public async Task ExecuteInTransactionAsync(Func<Task> action, CancellationToken ct = default)
        {
            var strategy = db.Database.CreateExecutionStrategy();

            await strategy.ExecuteAsync(async () =>
            {
                await using var tx = await db.Database.BeginTransactionAsync(
                    IsolationLevel.Serializable, ct);
                try
                {
                    await action();
                    await db.SaveChangesAsync(ct);
                    await tx.CommitAsync(ct);
                }
                catch
                {
                    await tx.RollbackAsync(ct);
                    throw;
                }
            });
        }
    }
}
