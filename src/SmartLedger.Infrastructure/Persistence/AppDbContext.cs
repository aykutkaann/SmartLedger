using Microsoft.EntityFrameworkCore;
using SmartLedger.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartLedger.Infrastructure.Persistence
{
    public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
    {

        public DbSet<User> Users => Set<User>();
        public DbSet<Account> Accounts => Set<Account>();
        public DbSet<Transaction> Transactions => Set<Transaction>();
        public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // For User

            modelBuilder.Entity<User>(e =>
            {
                e.ToTable("users");
                e.HasKey(u => u.Id);
                e.Property(u => u.Id).HasColumnName("id");
                e.Property(u => u.Email).HasColumnName("email").IsRequired();
                e.Property(u => u.PasswordHash).HasColumnName("password_hash").IsRequired();
                e.Property(u => u.Role).HasColumnName("role").HasDefaultValue("User");
                e.Property(u => u.CreatedAt).HasColumnName("created_at");
                e.Property(u => u.UpdatedAt).HasColumnName("updated_at");
                e.HasIndex(u => u.Email).IsUnique();


            });


            // For Account

            modelBuilder.Entity<Account>(e =>
            {
                e.ToTable("accounts");
                e.HasKey(a => a.Id);
                e.Property(a => a.Id).HasColumnName("id");
                e.Property(a => a.UserId).HasColumnName("user_id");
                e.Property(a => a.Iban).HasColumnName("iban").IsRequired();
                e.Property(a => a.Balance).HasColumnName("balance").HasPrecision(18, 2);
                e.Property(a => a.Currency).HasColumnName("currency").HasMaxLength(3);
                e.Property(a => a.Status).HasColumnName("status").HasConversion<string>().HasDefaultValue(AccountStatus.Active);
                e.Property(a => a.CreatedAt).HasColumnName("created_at");
                e.Property(a => a.UpdatedAt).HasColumnName("updated_at");


                e.HasIndex(a => a.Iban).IsUnique();
                e.HasIndex(a => a.UserId);

                e.HasOne(a => a.User)
                .WithMany(u => u.Accounts)
                .HasForeignKey(a => a.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            });


            // For Transaction

            modelBuilder.Entity<Transaction>(e =>
            {
                e.ToTable("transactions");
                e.HasKey(t => t.Id);
                e.Property(t => t.Id).HasColumnName("id");
                e.Property(t => t.FromAccountId).HasColumnName("from_account_id");
                e.Property(t => t.ToAccountId).HasColumnName("to_account_id");
                e.Property(t => t.Amount).HasColumnName("amount").HasPrecision(18, 2);
                e.Property(t => t.Currency).HasColumnName("currency").HasMaxLength(3);
                e.Property(t => t.Status).HasColumnName("status").HasConversion<string>();
                e.Property(t => t.Description).HasColumnName("description");
                e.Property(t => t.IpAddress).HasColumnName("ip_address");
                e.Property(t => t.FraudScore).HasColumnName("fraud_score");
                e.Property(t => t.FraudSignals).HasColumnName("fraud_signals")
                    .HasColumnType("jsonb")
                    .HasDefaultValue("{}");
                e.Property(t => t.CreatedAt).HasColumnName("created_at");
                e.Property(t => t.UpdatedAt).HasColumnName("updated_at");

                e.HasIndex(t => t.FromAccountId);
                e.HasIndex(t => t.ToAccountId);
                e.HasIndex(t => t.CreatedAt);
                e.HasIndex(t => t.Status);

                e.HasOne(t => t.FromAccount)
                 .WithMany(a => a.OutgoingTransactions)
                 .HasForeignKey(t => t.FromAccountId)
                 .OnDelete(DeleteBehavior.Restrict);

                e.HasOne(t => t.ToAccount)
                 .WithMany(a => a.IncomingTransactions)
                 .HasForeignKey(t => t.ToAccountId)
                 .OnDelete(DeleteBehavior.Restrict);
            });

            // ── RefreshToken
            modelBuilder.Entity<RefreshToken>(e =>
            {
                e.ToTable("refresh_tokens");
                e.HasKey(r => r.Id);
                e.Property(r => r.Id).HasColumnName("id");
                e.Property(r => r.UserId).HasColumnName("user_id");
                e.Property(r => r.TokenHash).HasColumnName("token_hash").IsRequired();
                e.Property(r => r.ExpiresAt).HasColumnName("expires_at");
                e.Property(r => r.RevokedAt).HasColumnName("revoked_at");
                e.Property(r => r.CreatedAt).HasColumnName("created_at");
                e.Property(r => r.UpdatedAt).HasColumnName("updated_at");

                e.HasIndex(r => r.TokenHash).IsUnique();
                e.HasIndex(r => r.UserId);

                e.HasOne(r => r.User)
                 .WithMany(u => u.RefreshTokens)
                 .HasForeignKey(r => r.UserId)
                 .OnDelete(DeleteBehavior.Cascade);
            });


            base.OnModelCreating(modelBuilder);
        }
     }
}
