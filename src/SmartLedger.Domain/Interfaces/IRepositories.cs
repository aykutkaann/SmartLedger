using SmartLedger.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartLedger.Domain.Interfaces
{
    public interface IUserRepository
    {
        Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default);
        Task<User?> GetByEmailAsync(string email, CancellationToken ct = default);
        Task AddAsync(User user, CancellationToken ct = default);
    }

    public interface IAccountRepository
    {
        Task<Account?> GetByIdAsync(Guid id, CancellationToken ct = default);
        Task<Account?> GetByIdWithLockAsync(Guid id, CancellationToken ct = default); 
        Task<IEnumerable<Account>> GetByUserIdAsync(Guid userId, CancellationToken ct = default);
        Task AddAsync(Account account, CancellationToken ct = default);
        Task<bool> ExistsAsync(Guid id, CancellationToken ct = default);
    }


    public interface ITransactionRepository
    {
        Task<Transaction?> GetByIdAsync(Guid id, CancellationToken ct = default);
        Task<IEnumerable<Transaction>> GetByAccountIdAsync(Guid accountId, int page, int pageSize, CancellationToken ct = default);
        Task<IEnumerable<Transaction>> GetPendingScoringAsync(CancellationToken ct = default);
        Task AddAsync(Transaction transaction, CancellationToken ct = default);
        Task<int> CountRecentByAccountAsync(Guid accountId, TimeSpan window, CancellationToken ct = default);
        Task<decimal> GetAverageAmountAsync(Guid accountId, int days, CancellationToken ct = default);
        Task<bool> HasPreviousTransferToAsync(Guid fromAccountId, Guid toAccountId, CancellationToken ct = default);
    }
    public interface IRefreshTokenRepository
    {
        Task<RefreshToken?> GetByHashAsync(string tokenHash, CancellationToken ct = default);
        Task AddAsync(RefreshToken token, CancellationToken ct = default);
        Task RevokeAllForUserAsync(Guid userId, CancellationToken ct = default);
    }

    public interface IUnitOfWork
    {
        Task<int> SaveChangesAsync(CancellationToken ct = default);
        Task ExecuteInTransactionAsync(Func<Task> action, CancellationToken ct = default);
    }


}
