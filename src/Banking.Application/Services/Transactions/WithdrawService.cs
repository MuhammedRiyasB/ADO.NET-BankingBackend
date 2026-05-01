using Banking.Application.Common;
using Banking.Application.DTOs.Transaction;
using Banking.Application.Interfaces;
using Banking.Application.Mappers;
using Banking.Application.Utilities;
using Banking.Application.Validators;
using Banking.Domain.Entities;
using Banking.Domain.Enums;

namespace Banking.Application.Services.Transactions;

/// <summary>
/// Handles withdrawal transactions.
/// </summary>
public sealed class WithdrawService : IWithdrawService
{
    private readonly IAccountRepository _accountRepository;
    private readonly ILedgerRepository _ledgerRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITransactionValidator _validator;
    private readonly IReferenceNormalizer _referenceNormalizer;
    private readonly IDailyDebitLimitChecker _limitChecker;
    private readonly ITransactionMapper _transactionMapper;
    private readonly IClock _clock;

    public WithdrawService(
        IAccountRepository accountRepository,
        ILedgerRepository ledgerRepository,
        IUnitOfWork unitOfWork,
        ITransactionValidator validator,
        IReferenceNormalizer referenceNormalizer,
        IDailyDebitLimitChecker limitChecker,
        ITransactionMapper transactionMapper,
        IClock clock)
    {
        _accountRepository = accountRepository;
        _ledgerRepository = ledgerRepository;
        _unitOfWork = unitOfWork;
        _validator = validator;
        _referenceNormalizer = referenceNormalizer;
        _limitChecker = limitChecker;
        _transactionMapper = transactionMapper;
        _clock = clock;
    }

    public async Task<Result<TransactionResponse>> WithdrawAsync(
        WithdrawRequest request,
        CancellationToken cancellationToken)
    {
        var validation = _validator.ValidateSingleAccountTransaction(
            request.AccountId,
            request.Amount,
            request.Reference,
            request.Narrative);
        if (validation is not null)
        {
            return Result<TransactionResponse>.Failure(validation.ErrorCode, validation.ErrorMessage);
        }

        var reference = _referenceNormalizer.Normalize(request.Reference);
        if (await _ledgerRepository.ReferenceExistsAsync(reference, cancellationToken))
        {
            return Result<TransactionResponse>.Failure(ErrorCodes.Conflict, "Reference has already been processed.");
        }

        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        var account = await _accountRepository.GetByIdForUpdateAsync(request.AccountId, cancellationToken);
        if (account is null)
        {
            return Result<TransactionResponse>.Failure(ErrorCodes.NotFound, "Account not found.");
        }

        if (!account.IsActive)
        {
            return Result<TransactionResponse>.Failure(ErrorCodes.BusinessRule, "Account is not active.");
        }

        var now = _clock.UtcNow;
        if (await _limitChecker.WouldExceedDailyLimitAsync(
            account.Id,
            account.DailyDebitLimit,
            request.Amount,
            now,
            cancellationToken))
        {
            return Result<TransactionResponse>.Failure(
                ErrorCodes.BusinessRule,
                "Daily debit limit exceeded.");
        }

        try
        {
            account.Debit(request.Amount, now);
        }
        catch (InvalidOperationException ex)
        {
            return Result<TransactionResponse>.Failure(ErrorCodes.BusinessRule, ex.Message);
        }

        var entry = LedgerEntry.Create(
            account.Id,
            reference,
            LedgerEntryType.Debit,
            request.Amount,
            account.Balance,
            request.Narrative.Trim(),
            now);

        await _accountRepository.UpdateAsync(account, cancellationToken);
        await _ledgerRepository.AddAsync(entry, cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);

        return Result<TransactionResponse>.Success(_transactionMapper.Map(entry, account.Currency));
    }
}
