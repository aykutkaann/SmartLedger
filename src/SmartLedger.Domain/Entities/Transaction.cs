using SmartLedger.Domain.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartLedger.Domain.Entities
{
    public enum TransactionStatus { Pending, Complated, Flagged, Rejected}
    public class Transaction : BaseEntity
    {
        public Guid FromAccountId { get; private set; }
        public Guid ToAccountId { get; private set; }
        public decimal Amount { get; private set; }
        public string Currency { get; private set; } = string.Empty;
        public TransactionStatus Status { get; private set; } = TransactionStatus.Pending;
        public string? Description { get; private set; }
        public string? IpAddress { get; private set; }


        // Fraud scoring — set by worker
        public int FraudScore { get; private set; }
        public string FraudSignals { get; private set; } = "{}"; // JSONBinary stored as string


        public Account FromAccount { get; private set; } = null!;
        public Account ToAccount { get; private set; } = null!;


        private Transaction() { }

        public static Transaction Create(
            Guid fromAccountId,
            Guid toAccountId,
            decimal amount,
            string currency,
            string? description = null,
            string? ipAddress = null
            )
        {
            if (fromAccountId == toAccountId)
                throw new ArgumentException("Cannot transfer to between same accounts.");
            if (amount <= 0)
                throw new ArgumentException("Ammount must be positive.");


            return new Transaction
            {
                FromAccountId = fromAccountId,
                ToAccountId = toAccountId,
                Amount = amount,
                Currency = currency.ToUpperInvariant(),
                Description = description,
                IpAddress = ipAddress,
                Status = TransactionStatus.Pending

            };

            
        }

        public void Create() { Status = TransactionStatus.Complated; UpdatedAt = DateTime.UtcNow; }
        public void Flag() { Status = TransactionStatus.Flagged; UpdatedAt = DateTime.UtcNow; }
        public void Reject() { Status = TransactionStatus.Rejected; UpdatedAt = DateTime.UtcNow; }


        public void SetFraudResult(int score, string signalsJson)
        {
            FraudScore = score;
            FraudSignals = signalsJson;
            Status = score >= 70 ? TransactionStatus.Flagged : TransactionStatus.Complated;
            UpdatedAt = DateTime.UtcNow;
        }


    }
}
