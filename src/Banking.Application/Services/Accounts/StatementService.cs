using Banking.Application.Common;
using Banking.Application.DTOs.Account;
using Banking.Application.Interfaces;
using Banking.Application.Mappers;

namespace Banking.Application.Services.Accounts;

/// <summary>
/// Handles account statement retrieval and formatting.
/// </summary>
public sealed class StatementService : IStatementService
{
    private readonly IAccountRepository _accountRepository;
    private readonly ILedgerRepository _ledgerRepository;
    private readonly IAccountMapper _accountMapper;

    public StatementService(
        IAccountRepository accountRepository,
        ILedgerRepository ledgerRepository,
        IAccountMapper accountMapper)
    {
        _accountRepository = accountRepository;
        _ledgerRepository = ledgerRepository;
        _accountMapper = accountMapper;
    }

    public async Task<Result<IReadOnlyCollection<StatementEntryResponse>>> GetStatementAsync(
        Guid accountId,
        DateTime? fromUtc,
        DateTime? toUtc,
        CancellationToken cancellationToken)
    {
        var account = await _accountRepository.GetByIdAsync(accountId, cancellationToken);
        if (account is null)
        {
            return Result<IReadOnlyCollection<StatementEntryResponse>>.Failure(
                ErrorCodes.NotFound,
                "Account not found.");
        }

        if (fromUtc.HasValue && toUtc.HasValue && fromUtc > toUtc)
        {
            return Result<IReadOnlyCollection<StatementEntryResponse>>.Failure(
                ErrorCodes.Validation,
                "fromUtc must be less than or equal to toUtc.");
        }

        var entries = await _ledgerRepository.GetStatementAsync(accountId, fromUtc, toUtc, cancellationToken);
        var mapped = entries
            .OrderByDescending(x => x.CreatedAtUtc)
            .Select(x => _accountMapper.MapStatement(x))
            .ToArray();

        return Result<IReadOnlyCollection<StatementEntryResponse>>.Success(mapped);
    }
}
