using SmartLedger.Domain.Entities;
using SmartLedger.Domain.Exceptions;
using SmartLedger.Domain.Services;
using Xunit;

namespace SmartLedger.UnitTests;

// ── Account tests ─────────────────────────────────────────────────────────────
public class AccountTests
{
    private static Account MakeAccount(decimal initialBalance = 0)
    {
        var account = Account.Create(Guid.NewGuid(), "USD", "TR000000000000000000000000");
        if (initialBalance > 0)
            account.Credit(initialBalance);
        return account;
    }

    [Fact]
    public void Debit_WithSufficientBalance_ReducesBalance()
    {
        var account = MakeAccount(500m);
        account.Debit(200m);
        Assert.Equal(300m, account.Balance);
    }

    [Fact]
    public void Debit_InsufficientFunds_ThrowsDomainException()
    {
        var account = MakeAccount(100m);
        Assert.Throws<DomainException>(() => account.Debit(200m));
    }

    [Fact]
    public void Debit_ZeroAmount_ThrowsDomainException()
    {
        var account = MakeAccount(500m);
        Assert.Throws<DomainException>(() => account.Debit(0m));
    }

    [Fact]
    public void Debit_NegativeAmount_ThrowsDomainException()
    {
        var account = MakeAccount(500m);
        Assert.Throws<DomainException>(() => account.Debit(-50m));
    }

    [Fact]
    public void Debit_FrozenAccount_ThrowsDomainException()
    {
        var account = MakeAccount(500m);
        account.Freeze();
        Assert.Throws<DomainException>(() => account.Debit(100m));
    }

    [Fact]
    public void Debit_ClosedAccount_ThrowsDomainException()
    {
        var account = MakeAccount(0m);
        account.Close();
        Assert.Throws<DomainException>(() => account.Debit(100m));
    }

    [Fact]
    public void Credit_ActiveAccount_IncreasesBalance()
    {
        var account = MakeAccount(100m);
        account.Credit(50m);
        Assert.Equal(150m, account.Balance);
    }

    [Fact]
    public void Credit_ZeroAmount_ThrowsDomainException()
    {
        var account = MakeAccount(100m);
        Assert.Throws<DomainException>(() => account.Credit(0m));
    }

    [Fact]
    public void Credit_FrozenAccount_ThrowsDomainException()
    {
        var account = MakeAccount(100m);
        account.Freeze();
        Assert.Throws<DomainException>(() => account.Credit(50m));
    }

    [Fact]
    public void Freeze_ActiveAccount_SetsStatusFrozen()
    {
        var account = MakeAccount(100m);
        account.Freeze();
        Assert.Equal(AccountStatus.Frozen, account.Status);
    }

    [Fact]
    public void Freeze_ClosedAccount_ThrowsDomainException()
    {
        var account = MakeAccount(0m);
        account.Close();
        Assert.Throws<DomainException>(() => account.Freeze());
    }

    [Fact]
    public void Close_ZeroBalance_SetsStatusClosed()
    {
        var account = MakeAccount(0m);
        account.Close();
        Assert.Equal(AccountStatus.Closed, account.Status);
    }

    [Fact]
    public void Close_NonZeroBalance_ThrowsDomainException()
    {
        var account = MakeAccount(100m);
        Assert.Throws<DomainException>(() => account.Close());
    }

    [Fact]
    public void NewAccount_HasZeroBalance()
    {
        var account = Account.Create(Guid.NewGuid(), "USD", "TR000000000000000000000000");
        Assert.Equal(0m, account.Balance);
    }

    [Fact]
    public void NewAccount_StatusIsActive()
    {
        var account = Account.Create(Guid.NewGuid(), "USD", "TR000000000000000000000000");
        Assert.Equal(AccountStatus.Active, account.Status);
    }

    [Fact]
    public void Create_CurrencyIsUppercased()
    {
        var account = Account.Create(Guid.NewGuid(), "usd", "TR000000000000000000000000");
        Assert.Equal("USD", account.Currency);
    }
}

// ── Transaction tests ─────────────────────────────────────────────────────────
public class TransactionTests
{
    [Fact]
    public void Create_ValidInputs_CreatesWithPendingStatus()
    {
        var tx = Transaction.Create(Guid.NewGuid(), Guid.NewGuid(), 100m, "USD");
        Assert.Equal(TransactionStatus.Pending, tx.Status);
    }

    [Fact]
    public void Create_SameAccountIds_ThrowsArgumentException()
    {
        var id = Guid.NewGuid();
        Assert.Throws<ArgumentException>(() =>
            Transaction.Create(id, id, 100m, "USD"));
    }

    [Fact]
    public void Create_ZeroAmount_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
            Transaction.Create(Guid.NewGuid(), Guid.NewGuid(), 0m, "USD"));
    }

    [Fact]
    public void Create_NegativeAmount_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
            Transaction.Create(Guid.NewGuid(), Guid.NewGuid(), -1m, "USD"));
    }

    [Fact]
    public void Create_CurrencyIsUppercased()
    {
        var tx = Transaction.Create(Guid.NewGuid(), Guid.NewGuid(), 100m, "usd");
        Assert.Equal("USD", tx.Currency);
    }

    [Fact]
    public void SetFraudResult_ScoreAbove70_SetsStatusFlagged()
    {
        var tx = Transaction.Create(Guid.NewGuid(), Guid.NewGuid(), 1000m, "USD");
        tx.SetFraudResult(75, "{}");
        Assert.Equal(TransactionStatus.Flagged, tx.Status);
    }

    [Fact]
    public void SetFraudResult_ScoreExactly70_SetsStatusFlagged()
    {
        var tx = Transaction.Create(Guid.NewGuid(), Guid.NewGuid(), 1000m, "USD");
        tx.SetFraudResult(70, "{}");
        Assert.Equal(TransactionStatus.Flagged, tx.Status);
    }

    [Fact]
    public void SetFraudResult_ScoreBelow70_SetsStatusCompleted()
    {
        var tx = Transaction.Create(Guid.NewGuid(), Guid.NewGuid(), 100m, "USD");
        tx.SetFraudResult(69, "{}");
        Assert.Equal(TransactionStatus.Complated, tx.Status);
    }

    [Fact]
    public void SetFraudResult_ZeroScore_SetsStatusCompleted()
    {
        var tx = Transaction.Create(Guid.NewGuid(), Guid.NewGuid(), 100m, "USD");
        tx.SetFraudResult(0, "{}");
        Assert.Equal(TransactionStatus.Complated, tx.Status);
    }

    [Fact]
    public void SetFraudResult_StoresFraudScore()
    {
        var tx = Transaction.Create(Guid.NewGuid(), Guid.NewGuid(), 100m, "USD");
        tx.SetFraudResult(45, "{\"velocity\":{\"points\":45}}");
        Assert.Equal(45, tx.FraudScore);
    }

    [Fact]
    public void SetFraudResult_StoresFraudSignals()
    {
        var tx = Transaction.Create(Guid.NewGuid(), Guid.NewGuid(), 100m, "USD");
        var signals = "{\"velocity\":{\"points\":30}}";
        tx.SetFraudResult(30, signals);
        Assert.Equal(signals, tx.FraudSignals);
    }

    [Fact]
    public void Complete_SetsStatusCompleted()
    {
        var tx = Transaction.Create(Guid.NewGuid(), Guid.NewGuid(), 100m, "USD");
        Assert.Equal(TransactionStatus.Complated, tx.Status);
    }

    [Fact]
    public void Flag_SetsStatusFlagged()
    {
        var tx = Transaction.Create(Guid.NewGuid(), Guid.NewGuid(), 100m, "USD");
        tx.Flag();
        Assert.Equal(TransactionStatus.Flagged, tx.Status);
    }

    [Fact]
    public void Reject_SetsStatusRejected()
    {
        var tx = Transaction.Create(Guid.NewGuid(), Guid.NewGuid(), 100m, "USD");
        tx.Reject();
        Assert.Equal(TransactionStatus.Rejected, tx.Status);
    }
}

// ── User tests ────────────────────────────────────────────────────────────────
public class UserTests
{
    [Fact]
    public void Create_ValidInputs_SetsEmailLowercase()
    {
        var user = User.Create("TEST@SMARTLEDGER.IO", "hashedpassword");
        Assert.Equal("test@smartledger.io", user.Email);
    }

    [Fact]
    public void Create_DefaultRole_IsUser()
    {
        var user = User.Create("test@smartledger.io", "hashedpassword");
        Assert.Equal("User", user.Role);
    }

    [Fact]
    public void MakeAdmin_SetsRoleAdmin()
    {
        var user = User.Create("test@smartledger.io", "hashedpassword");
        user.MakeAdmin();
        Assert.Equal("Admin", user.Role);
    }

    [Fact]
    public void Create_EmptyEmail_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
            User.Create("", "hashedpassword"));
    }

    [Fact]
    public void Create_EmptyPasswordHash_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
            User.Create("test@smartledger.io", ""));
    }
}

// ── RefreshToken tests ────────────────────────────────────────────────────────
public class RefreshTokenTests
{
    [Fact]
    public void Create_IsActiveByDefault()
    {
        var token = RefreshToken.Create(Guid.NewGuid(), "somehash");
        Assert.True(token.IsActive);
    }

    [Fact]
    public void Revoke_SetsRevokedAt()
    {
        var token = RefreshToken.Create(Guid.NewGuid(), "somehash");
        token.Revoke();
        Assert.NotNull(token.RevokedAt);
    }

    [Fact]
    public void Revoke_IsActiveReturnsFalse()
    {
        var token = RefreshToken.Create(Guid.NewGuid(), "somehash");
        token.Revoke();
        Assert.False(token.IsActive);
    }

    [Fact]
    public void Create_ExpiresInFuture()
    {
        var token = RefreshToken.Create(Guid.NewGuid(), "somehash", expiryDays: 7);
        Assert.True(token.ExpiresAt > DateTime.UtcNow);
    }
}

// ── IBAN generator tests ──────────────────────────────────────────────────────
public class IbanGeneratorTests
{
    private readonly IIbanGenerator _gen = new IbanGenerator();

    [Fact]
    public void Generate_DefaultCountry_StartsWithTR()
    {
        var iban = _gen.Generate();
        Assert.StartsWith("TR", iban);
    }

    [Fact]
    public void Generate_CustomCountry_StartsWithCountryCode()
    {
        var iban = _gen.Generate("DE");
        Assert.StartsWith("DE", iban);
    }

    [Fact]
    public void Generate_TwoCallsProduceDifferentIbans()
    {
        Assert.NotEqual(_gen.Generate(), _gen.Generate());
    }

    [Fact]
    public void Generate_HasExpectedLength()
    {
        var iban = _gen.Generate();
        Assert.Equal(25, iban.Length);
    }

    [Fact]
    public void Generate_CheckDigitsAreNumeric()
    {
        var iban = _gen.Generate();
        var checkDigits = iban.Substring(2, 2);
        Assert.True(int.TryParse(checkDigits, out _));
    }
}