using Banking.Application.Common;
using Banking.Application.DTOs.Account;
using Banking.Application.DTOs.Transaction;
using Banking.Application.Interfaces;
using Banking.Application.Mappers;
using Banking.Application.Services;
using Banking.Application.Services.Transactions;
using Banking.Application.Utilities;
using Banking.Application.Validators;
using Banking.Domain.Entities;
using Banking.Domain.Enums;
using Xunit;

namespace Banking.Application.Tests;

public sealed class ServiceReliabilityTests
{
    [Fact]
    public async Task OpenAccountAsync_Retries_AfterDuplicateAccountNumber()
    {
        var customer = Customer.CreateNew("Jane Doe", "jane@example.com", "12345678", new DateTime(2026, 5, 2, 10, 0, 0, DateTimeKind.Utc));
        var unitOfWork = new RecordingUnitOfWork();
        var accountRepository = new RetryingAccountRepository(1);
        var service = new AccountService(
            accountRepository,
            new StubCustomerRepository(customer),
            new SequenceAccountNumberGenerator("1111111111", "2222222222"),
            unitOfWork,
            new CurrencyValidator(),
            new AmountValidator(),
            new AccountMapper(),
            new FixedClock(new DateTime(2026, 5, 2, 10, 0, 0, DateTimeKind.Utc)));

        var result = await service.OpenAccountAsync(
            new OpenAccountRequest(customer.Id, AccountType.Savings, "USD", 2500m),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal("2222222222", result.Value!.AccountNumber);
        Assert.Equal(2, accountRepository.AddAttempts);
        Assert.Equal(2, unitOfWork.BeginCalls);
        Assert.Equal(1, unitOfWork.RollbackCalls);
        Assert.Equal(1, unitOfWork.CommitCalls);
    }

    [Fact]
    public async Task DepositAsync_RollsBack_WhenReferenceAlreadyExistsAtInsertTime()
    {
        var account = Account.OpenNew(Guid.NewGuid(), "1234567890", AccountType.Savings, "USD", 5000m, new DateTime(2026, 5, 2, 10, 0, 0, DateTimeKind.Utc));
        var unitOfWork = new RecordingUnitOfWork();
        var service = new DepositService(
            new StubAccountRepository(account),
            new DuplicateLedgerRepository(),
            unitOfWork,
            new TransactionValidator(new AmountValidator()),
            new ReferenceNormalizer(),
            new TransactionMapper(),
            new FixedClock(new DateTime(2026, 5, 2, 10, 30, 0, DateTimeKind.Utc)));

        var result = await service.DepositAsync(
            new DepositRequest(account.Id, 250m, "Cash deposit", "dup-ref-01"),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorCodes.Conflict, result.ErrorCode);
        Assert.Equal(1, unitOfWork.BeginCalls);
        Assert.Equal(1, unitOfWork.RollbackCalls);
        Assert.Equal(0, unitOfWork.CommitCalls);
    }

    [Fact]
    public async Task TransferAsync_ReturnsExistingCompletedTransfer_WhenExternalReferenceConflicts()
    {
        var fromAccount = Account.OpenNew(Guid.NewGuid(), "1111111111", AccountType.Current, "USD", 5000m, new DateTime(2026, 5, 2, 9, 0, 0, DateTimeKind.Utc));
        fromAccount.Credit(800m, new DateTime(2026, 5, 2, 9, 5, 0, DateTimeKind.Utc));
        var toAccount = Account.OpenNew(Guid.NewGuid(), "2222222222", AccountType.Savings, "USD", 5000m, new DateTime(2026, 5, 2, 9, 0, 0, DateTimeKind.Utc));

        var existingTransfer = new Transfer(
            Guid.NewGuid(),
            "EXT-123",
            fromAccount.Id,
            toAccount.Id,
            150m,
            "USD",
            "Rent",
            TransferStatus.Completed,
            new DateTime(2026, 5, 2, 9, 10, 0, DateTimeKind.Utc),
            new DateTime(2026, 5, 2, 9, 11, 0, DateTimeKind.Utc));

        var unitOfWork = new RecordingUnitOfWork();
        var service = new TransferService(
            new DualAccountRepository(fromAccount, toAccount),
            new NoOpLedgerRepository(),
            new ConflictingTransferRepository(existingTransfer),
            unitOfWork,
            new TransactionValidator(new AmountValidator()),
            new ReferenceNormalizer(),
            new NeverExceededLimitChecker(),
            new TransferMapper(),
            new FixedClock(new DateTime(2026, 5, 2, 11, 0, 0, DateTimeKind.Utc)));

        var result = await service.TransferAsync(
            new TransferRequest(fromAccount.Id, toAccount.Id, 150m, "Rent", "ext-123"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(existingTransfer.Id, result.Value!.Id);
        Assert.Equal("EXT-123", result.Value.ExternalReference);
        Assert.Equal(1, unitOfWork.RollbackCalls);
        Assert.Equal(0, unitOfWork.CommitCalls);
    }

    private sealed class RecordingUnitOfWork : IUnitOfWork
    {
        public int BeginCalls { get; private set; }
        public int CommitCalls { get; private set; }
        public int RollbackCalls { get; private set; }

        public Task BeginTransactionAsync(CancellationToken cancellationToken = default)
        {
            BeginCalls++;
            return Task.CompletedTask;
        }

        public Task CommitAsync(CancellationToken cancellationToken = default)
        {
            CommitCalls++;
            return Task.CompletedTask;
        }

        public Task RollbackAsync(CancellationToken cancellationToken = default)
        {
            RollbackCalls++;
            return Task.CompletedTask;
        }
    }

    private sealed class FixedClock : IClock
    {
        public FixedClock(DateTime utcNow)
        {
            UtcNow = utcNow;
        }

        public DateTime UtcNow { get; }
    }

    private sealed class SequenceAccountNumberGenerator : IAccountNumberGenerator
    {
        private readonly Queue<string> _values;

        public SequenceAccountNumberGenerator(params string[] values)
        {
            _values = new Queue<string>(values);
        }

        public string Generate()
        {
            return _values.Dequeue();
        }
    }

    private sealed class StubCustomerRepository : ICustomerRepository
    {
        private readonly Customer _customer;

        public StubCustomerRepository(Customer customer)
        {
            _customer = customer;
        }

        public Task<Customer?> GetByIdAsync(Guid customerId, CancellationToken cancellationToken)
        {
            return Task.FromResult<Customer?>(_customer.Id == customerId ? _customer : null);
        }

        public Task<Customer?> GetByEmailAsync(string email, CancellationToken cancellationToken)
        {
            return Task.FromResult<Customer?>(string.Equals(_customer.Email, email, StringComparison.OrdinalIgnoreCase) ? _customer : null);
        }

        public Task AddAsync(Customer customer, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }
    }

    private sealed class RetryingAccountRepository : IAccountRepository
    {
        private int _remainingFailures;

        public RetryingAccountRepository(int remainingFailures)
        {
            _remainingFailures = remainingFailures;
        }

        public int AddAttempts { get; private set; }

        public Task<Account?> GetByIdAsync(Guid accountId, CancellationToken cancellationToken) => Task.FromResult<Account?>(null);

        public Task<Account?> GetByIdForUpdateAsync(Guid accountId, CancellationToken cancellationToken) => Task.FromResult<Account?>(null);

        public Task<Account?> GetByAccountNumberAsync(string accountNumber, CancellationToken cancellationToken) => Task.FromResult<Account?>(null);

        public Task AddAsync(Account account, CancellationToken cancellationToken)
        {
            AddAttempts++;
            if (_remainingFailures > 0)
            {
                _remainingFailures--;
                throw new DuplicateResourceException("account", "account number", account.AccountNumber);
            }

            return Task.CompletedTask;
        }

        public Task UpdateAsync(Account account, CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class StubAccountRepository : IAccountRepository
    {
        private readonly Account _account;

        public StubAccountRepository(Account account)
        {
            _account = account;
        }

        public Task<Account?> GetByIdAsync(Guid accountId, CancellationToken cancellationToken) => Task.FromResult<Account?>(_account);

        public Task<Account?> GetByIdForUpdateAsync(Guid accountId, CancellationToken cancellationToken) => Task.FromResult<Account?>(_account);

        public Task<Account?> GetByAccountNumberAsync(string accountNumber, CancellationToken cancellationToken) => Task.FromResult<Account?>(null);

        public Task AddAsync(Account account, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task UpdateAsync(Account account, CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class DualAccountRepository : IAccountRepository
    {
        private readonly Account _first;
        private readonly Account _second;

        public DualAccountRepository(Account first, Account second)
        {
            _first = first;
            _second = second;
        }

        public Task<Account?> GetByIdAsync(Guid accountId, CancellationToken cancellationToken)
        {
            return Task.FromResult<Account?>(accountId == _first.Id ? _first : accountId == _second.Id ? _second : null);
        }

        public Task<Account?> GetByIdForUpdateAsync(Guid accountId, CancellationToken cancellationToken)
        {
            return GetByIdAsync(accountId, cancellationToken);
        }

        public Task<Account?> GetByAccountNumberAsync(string accountNumber, CancellationToken cancellationToken) => Task.FromResult<Account?>(null);

        public Task AddAsync(Account account, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task UpdateAsync(Account account, CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class DuplicateLedgerRepository : ILedgerRepository
    {
        public Task<bool> ReferenceExistsAsync(string reference, CancellationToken cancellationToken) => Task.FromResult(false);

        public Task AddAsync(LedgerEntry entry, CancellationToken cancellationToken)
        {
            throw new DuplicateResourceException("ledger entry", "reference", entry.Reference);
        }

        public Task<IReadOnlyCollection<LedgerEntry>> GetStatementAsync(
            Guid accountId,
            DateTime? fromUtc,
            DateTime? toUtc,
            CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyCollection<LedgerEntry>>(Array.Empty<LedgerEntry>());
        }

        public Task<decimal> GetTotalDebitsForPeriodAsync(Guid accountId, DateTime startUtc, DateTime endUtc, CancellationToken cancellationToken)
        {
            return Task.FromResult(0m);
        }
    }

    private sealed class NoOpLedgerRepository : ILedgerRepository
    {
        public Task<bool> ReferenceExistsAsync(string reference, CancellationToken cancellationToken) => Task.FromResult(false);

        public Task AddAsync(LedgerEntry entry, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task<IReadOnlyCollection<LedgerEntry>> GetStatementAsync(
            Guid accountId,
            DateTime? fromUtc,
            DateTime? toUtc,
            CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyCollection<LedgerEntry>>(Array.Empty<LedgerEntry>());
        }

        public Task<decimal> GetTotalDebitsForPeriodAsync(Guid accountId, DateTime startUtc, DateTime endUtc, CancellationToken cancellationToken)
        {
            return Task.FromResult(0m);
        }
    }

    private sealed class ConflictingTransferRepository : ITransferRepository
    {
        private readonly Transfer _existingTransfer;

        public ConflictingTransferRepository(Transfer existingTransfer)
        {
            _existingTransfer = existingTransfer;
        }

        public Task<Transfer?> GetByExternalReferenceAsync(string externalReference, CancellationToken cancellationToken)
        {
            return Task.FromResult<Transfer?>(_existingTransfer);
        }

        public Task AddAsync(Transfer transfer, CancellationToken cancellationToken)
        {
            throw new DuplicateResourceException("transfer", "external reference", transfer.ExternalReference);
        }

        public Task UpdateAsync(Transfer transfer, CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class NeverExceededLimitChecker : IDailyDebitLimitChecker
    {
        public Task<bool> WouldExceedDailyLimitAsync(
            Guid accountId,
            decimal dailyDebitLimit,
            decimal amountToDebit,
            DateTime utcNow,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(false);
        }
    }
}
