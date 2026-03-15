using MediatR;
using SmartLedger.Application.Accounts;
using SmartLedger.Domain.Entities;
using SmartLedger.Domain.Interfaces;
using SmartLedger.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartLedger.Application.Transfers
{
    //--DTOs

    public record TransferRequest(Guid FromAccountId, Guid ToAccountId, decimal Amount, string? Description);
    public record TransferDto(Guid Id, Guid FromAccountId, Guid ToAccountId, decimal Amount, string Currency, string Status, DateTime CreatedAt);



    // ── Initiate transfer

    public record InitiateTransferCommand(
        Guid RequestingUserId,
        Guid FromAccountId,
        Guid ToAccountId,
        decimal Amount,
        string? Description,
        string? IpAddress) : IRequest<TransferDto>;

    public class InitiateTransferHandler(
        IAccountRepository accounts,
        ITransactionRepository transactions,
        IUnitOfWork uow) : IRequestHandler<InitiateTransferCommand, TransferDto>
    {
        public async Task<TransferDto> Handle(InitiateTransferCommand cmd, CancellationToken ct)
        {
            Transaction? tx = null;

           
            await uow.ExecuteInTransactionAsync(async () =>   //Prevents RACE CONDITION
            {
                // 1. Load accounts with SELECT FOR UPDATE 
                var from = await accounts.GetByIdWithLockAsync(cmd.FromAccountId, ct)
                    ?? throw new KeyNotFoundException("Source account not found.");
                var to = await accounts.GetByIdWithLockAsync(cmd.ToAccountId, ct)
                    ?? throw new KeyNotFoundException("Destination account not found.");

                // 2. Ownership check
                if (from.UserId != cmd.RequestingUserId)
                    throw new UnauthorizedAccessException("You do not own the source account.");

                // 3. Currency match
                if (from.Currency != to.Currency)
                    throw new InvalidOperationException("Cross-currency transfers are not supported.");

                // 4. Domain rules: balance check, status check (throws DomainException if violated)
                from.Debit(cmd.Amount);
                to.Credit(cmd.Amount);

                // 5. Create transaction record (status = Pending)
                tx = Transaction.Create(
                    cmd.FromAccountId,
                    cmd.ToAccountId,
                    cmd.Amount,
                    from.Currency,
                    cmd.Description,
                    cmd.IpAddress);

                await transactions.AddAsync(tx, ct);
                
            }, ct);

            if (tx is null)
                throw new InvalidOperationException("Transaction was not created.");


            return ToDto(tx!);
        }
        private static TransferDto ToDto(Transaction t) =>
        new(t.Id, t.FromAccountId, t.ToAccountId, t.Amount, t.Currency, t.Status.ToString(), t.CreatedAt);
    }


    // ── Get transaction history
    public record TransactionPageDto(IEnumerable<TransferDto> Items, int Page, int PageSize);

    public record GetTransactionHistoryQuery(Guid AccountId, Guid RequestingUserId, int Page = 1, int PageSize = 20)
        : IRequest<TransactionPageDto>;

    public class GetTransactionHistoryHandler(IAccountRepository accounts, ITransactionRepository transactions)
        : IRequestHandler<GetTransactionHistoryQuery, TransactionPageDto>
    {
        public async Task<TransactionPageDto> Handle(GetTransactionHistoryQuery query, CancellationToken ct)
        {
            var account = await accounts.GetByIdAsync(query.AccountId, ct)
                ?? throw new KeyNotFoundException("Account not found.");

            if (account.UserId != query.RequestingUserId)
                throw new UnauthorizedAccessException("You can only view your own account history.");

            var items = await transactions.GetByAccountIdAsync(query.AccountId, query.Page, query.PageSize, ct);

            return new TransactionPageDto(
             items.Select(t => new TransferDto(t.Id, t.FromAccountId, t.ToAccountId, t.Amount, t.Currency, t.Status.ToString(), t.CreatedAt)),
             query.Page,
             query.PageSize);
        }
    }


    // ── Get single transfer

    public record TransferDetailDto(
    Guid Id, Guid FromAccountId, Guid ToAccountId,
    decimal Amount, string Currency, string Status,
    int FraudScore, string FraudSignals, DateTime CreatedAt);

    public record GetTransferQuery(Guid TransactionId, Guid RequestingUserId) : IRequest<TransferDetailDto>;

    public class GetTransferHandler(
    ITransactionRepository transactions,
    IAccountRepository accounts) : IRequestHandler<GetTransferQuery, TransferDetailDto>
    {
        public async Task<TransferDetailDto> Handle(GetTransferQuery query, CancellationToken ct)
        {
            var tx = await transactions.GetByIdAsync(query.TransactionId, ct)
           ?? throw new KeyNotFoundException("Transaction not found.");

            
            var from = await accounts.GetByIdAsync(tx.FromAccountId, ct);
            if (from?.UserId != query.RequestingUserId)
                throw new UnauthorizedAccessException("Access denied.");

            return new TransferDetailDto(
                tx.Id, tx.FromAccountId, tx.ToAccountId,
                tx.Amount, tx.Currency, tx.Status.ToString(),
                tx.FraudScore, tx.FraudSignals, tx.CreatedAt);
        }
    }
}
