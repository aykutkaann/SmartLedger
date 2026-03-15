using SmartLedger.Domain.Common;
using SmartLedger.Domain.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;

namespace SmartLedger.Domain.Entities
{
    public enum AccountStatus { Active, Frozen, Closed}
    public class Account : BaseEntity
    {
        public Guid UserId { get; private set; }
        public string Iban { get; private set; } = string.Empty;
        public decimal Balance { get; private set; }
        public string Currency { get; private set; } = string.Empty;
        public AccountStatus Status { get; private set; } = AccountStatus.Active;

        public User User { get; private set; } = null;
        public ICollection<Transaction> OutgoingTransactions { get; private set; } = new List<Transaction>();
        public ICollection<Transaction> IncomingTransactions { get; private set; } = new List<Transaction>();



        private Account () { }


        public static Account Create( Guid userId, string currency, string iban)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(currency);
            ArgumentException.ThrowIfNullOrWhiteSpace(iban);

            return new Account
            {
                UserId = userId,
                Currency = currency.ToUpperInvariant(),
                Iban = iban,
                Balance = 0
            };
        }

        public void Debit(decimal amount)
        {
            if(Status != AccountStatus.Active)
                throw new DomainException($"Cannot debit a {Status} account.");
            if (amount <= 0)
                throw new DomainException("Credit Amount Must Be Positive");

            Balance += amount;
            UpdatedAt = DateTime.UtcNow;
        }

        public void Freeze()
        {
            if (Status == AccountStatus.Closed)
                throw new DomainException("Cannot freeze a closed account");

            Status = AccountStatus.Frozen;
            UpdatedAt = DateTime.UtcNow;
        }

        public void Closed()
        {
            if (Balance != 0)
                throw new DomainException("Cannot close an account with a non-zero balance");

            Status = AccountStatus.Closed;
            UpdatedAt = DateTime.UtcNow;
        }
    }
}
