using Dapper;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using SmartLedger.Domain.Entities;
using SmartLedger.Domain.Interfaces;
using SmartLedger.Infrastructure.Persistence;

namespace SmartLedger.Infrastructure.Repositories;

// ── UserRepository
public class UserRepository(AppDbContext db) : IUserRepository
{
    public Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => db.Users.FirstOrDefaultAsync(u => u.Id == id, ct);

    public Task<User?> GetByEmailAsync(string email, CancellationToken ct = default)
        => db.Users.FirstOrDefaultAsync(u => u.Email == email.ToLowerInvariant(), ct);

    public async Task AddAsync(User user, CancellationToken ct = default)
        => await db.Users.AddAsync(user, ct);
}

// AccountRepository
public class AccountRepository(AppDbContext db) : IAccountRepository
{
    public Task<Account?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => db.Accounts.FirstOrDefaultAsync(a => a.Id == id, ct);

    

    public Task<Account?> GetByIdWithLockAsync(Guid id, CancellationToken ct = default)
        => db.Accounts
             .FromSqlRaw("SELECT * FROM accounts WHERE id = {0} FOR UPDATE", id)
             .FirstOrDefaultAsync(ct);

    public Task<IEnumerable<Account>> GetByUserIdAsync(Guid userId, CancellationToken ct = default)
        => Task.FromResult<IEnumerable<Account>>(
            db.Accounts.Where(a => a.UserId == userId).AsEnumerable());

    public async Task AddAsync(Account account, CancellationToken ct = default)
        => await db.Accounts.AddAsync(account, ct);

    public Task<bool> ExistsAsync(Guid id, CancellationToken ct = default)
        => db.Accounts.AnyAsync(a => a.Id == id, ct);
}

// TransactionRepository 
// Reads use Dapper with raw SQL to demonstrate PostgreSQL-specific features.
public class TransactionRepository(AppDbContext db, NpgsqlDataSource dataSource) : ITransactionRepository
{
    public Task<Transaction?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => db.Transactions
             .Include(t => t.FromAccount)
             .Include(t => t.ToAccount)
             .FirstOrDefaultAsync(t => t.Id == id, ct);

 
    public async Task<IEnumerable<Transaction>> GetByAccountIdAsync(
        Guid accountId, int page, int pageSize, CancellationToken ct = default)
    {
        const string sql = """
            SELECT * FROM transactions
            WHERE from_account_id = @AccountId OR to_account_id = @AccountId
            ORDER BY created_at DESC
            LIMIT @PageSize OFFSET @Offset
            """;

        await using var conn = await dataSource.OpenConnectionAsync(ct);
        return await conn.QueryAsync<Transaction>(sql, new
        {
            AccountId = accountId,
            PageSize = pageSize,
            Offset = (page - 1) * pageSize
        });
    }

    public async Task<IEnumerable<Transaction>> GetPendingScoringAsync(CancellationToken ct = default)
    {
        const string sql = "SELECT * FROM transactions WHERE status = 'Pending' ORDER BY created_at LIMIT 100";
        await using var conn = await dataSource.OpenConnectionAsync(ct);
        return await conn.QueryAsync<Transaction>(sql);
    }

    public async Task AddAsync(Transaction transaction, CancellationToken ct = default)
        => await db.Transactions.AddAsync(transaction, ct);

  
    public async Task<int> CountRecentByAccountAsync(Guid accountId, TimeSpan window, CancellationToken ct = default)
    {
        const string sql = """
            SELECT COUNT(*)::int FROM transactions
            WHERE from_account_id = @AccountId
              AND created_at >= NOW() - @Window::interval
            """;

        await using var conn = await dataSource.OpenConnectionAsync(ct);
        return await conn.ExecuteScalarAsync<int>(sql, new
        {
            AccountId = accountId,
            Window = $"{window.TotalSeconds} seconds"
        });
    }

  
    public async Task<decimal> GetAverageAmountAsync(Guid accountId, int days, CancellationToken ct = default)
    {
        const string sql = """
            SELECT COALESCE(AVG(amount), 0) FROM transactions
            WHERE from_account_id = @AccountId
              AND created_at >= NOW() - (@Days || ' days')::interval
              AND status IN ('Completed', 'Flagged')
            """;

        await using var conn = await dataSource.OpenConnectionAsync(ct);
        return await conn.ExecuteScalarAsync<decimal>(sql, new { AccountId = accountId, Days = days });
    }

    public async Task<bool> HasPreviousTransferToAsync(Guid fromAccountId, Guid toAccountId, CancellationToken ct = default)
    {
        const string sql = """
            SELECT EXISTS (
                SELECT 1 FROM transactions
                WHERE from_account_id = @From AND to_account_id = @To
                  AND status IN ('Completed')
            )
            """;

        await using var conn = await dataSource.OpenConnectionAsync(ct);
        return await conn.ExecuteScalarAsync<bool>(sql, new { From = fromAccountId, To = toAccountId });
    }
}

// RefreshTokenRepository
public class RefreshTokenRepository(AppDbContext db) : IRefreshTokenRepository
{
    public Task<RefreshToken?> GetByHashAsync(string tokenHash, CancellationToken ct = default)
        => db.RefreshTokens
             .Include(r => r.User)
             .FirstOrDefaultAsync(r => r.TokenHash == tokenHash, ct);

    public async Task AddAsync(RefreshToken token, CancellationToken ct = default)
        => await db.RefreshTokens.AddAsync(token, ct);

    public async Task RevokeAllForUserAsync(Guid userId, CancellationToken ct = default)
    {
        var tokens = await db.RefreshTokens
            .Where(r => r.UserId == userId && r.RevokedAt == null)
            .ToListAsync(ct);

        foreach (var t in tokens) t.Revoke();
    }
}