using SmartLedger.Domain.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartLedger.Domain.Entities
{
    public class RefreshToken : BaseEntity
    {

        public Guid UserId { get; private set; }
        public string TokenHash { get; private set; } = string.Empty;
        public DateTime ExpiresAt { get; private set; }
        public DateTime? RevokedAt { get; private set; }

        public User User { get; private set; } = null!;


        public bool IsActive => RevokedAt is null && DateTime.UtcNow < ExpiresAt;

        private RefreshToken() { }

        public static RefreshToken Create(Guid userId, string tokenHash, int expiryDays = 7)
        {
            return new RefreshToken
            {
                UserId = userId,
                TokenHash = tokenHash,
                ExpiresAt = DateTime.UtcNow.AddDays(expiryDays)
            };
        }


        public void Revoke() => RevokedAt = DateTime.UtcNow;
    }
}
