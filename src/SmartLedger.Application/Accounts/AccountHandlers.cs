using MediatR;
using SmartLedger.Domain.Entities;
using SmartLedger.Domain.Interfaces;
using SmartLedger.Domain.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartLedger.Application.Accounts
{
    // DTOs

    public record AccountDto(Guid Id, string Iban, decimal Balance, string Currency, string Status, DateTime CreatedAt);

    // Create Account

    public record CreateAccountCommand(Guid UserId, string Currency) : IRequest<AccountDto>;


    public class CreateAccountHandler(
        IAccountRepository accounts,
        IUnitOfWork uow,
        IIbanGenerator ibanGenerator) : IRequestHandler<CreateAccountCommand, AccountDto>
    {
        public async Task<AccountDto> Handle(CreateAccountCommand cmd, CancellationToken ct)
        {
            var iban = ibanGenerator.Generate();
            var account = Account.Create(cmd.UserId, cmd.Currency, iban);
            await accounts.AddAsync(account, ct);
            await uow.SaveChangesAsync(ct);

            return MappingExtensions.ToDto(account);
        }
    }

    // ── Get accounts for user

    public record GetMyAccountQuery(Guid UserId) : IRequest<IEnumerable<AccountDto>>;

    public class GetMyAccountHandler(
        IAccountRepository accounts) : IRequestHandler<GetMyAccountQuery, IEnumerable<AccountDto>>
    {
        public async Task<IEnumerable<AccountDto>> Handle(GetMyAccountQuery query, CancellationToken ct)
        {
            var list = await accounts.GetByUserIdAsync(query.UserId, ct);
            return list.Select(MappingExtensions.ToDto);
        }
    }


    // ── Close account

    public record CloseAccountCommand(Guid AccountId, Guid RequestingUserId) : IRequest;

    public class CloseAccountHandler(IAccountRepository accounts, IUnitOfWork uow): IRequestHandler<CloseAccountCommand>
    {
        public async Task Handle(CloseAccountCommand cmd, CancellationToken ct)
        {
            var account = await accounts.GetByIdAsync(cmd.AccountId, ct);

            if (account is null)
                throw new KeyNotFoundException("Account not found.");

            if (account.UserId != cmd.RequestingUserId)
                throw new UnauthorizedAccessException("You do not own this account.");

            account.Close();
            await uow.SaveChangesAsync(ct);
        }
    }


    // ── Shared
    internal static class MappingExtensions
    {
        internal static AccountDto ToDto(Account a) =>
            new(a.Id, a.Iban, a.Balance, a.Currency, a.Status.ToString(), a.CreatedAt);
    }

}
