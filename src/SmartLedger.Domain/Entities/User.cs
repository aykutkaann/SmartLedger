using SmartLedger.Domain.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;

namespace SmartLedger.Domain.Entities
{
    public class User : BaseEntity
    {
        public string Email { get; private set; } = string.Empty;
        public string PasswordHash { get; private set; } = string.Empty;
        public string Role { get; private set; } = "User";


        public ICollection<Account> Accounts { get; private set; } = new List<Account>();
        public ICollection<RefreshToken> RefreshTokens { get; private set; } = new List<RefreshToken>();


        private User() { }

        public static User Create(string email, string passwordHash)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(email);
            ArgumentException.ThrowIfNullOrWhiteSpace(passwordHash);


            return new User
            {
                Email = email.ToLowerInvariant().Trim(),
                PasswordHash = passwordHash

            };
        }

        public void MakeAdmin() => Role = "Admin";
    }
}
