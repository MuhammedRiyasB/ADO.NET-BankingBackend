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
/// Handles account-to-account transfers.
/// </summary>
public sealed class TransferService : ITransferService
{
    private readonly IAccountRepository _accountRepository;
    private readonly ILedgerRepository _ledgerRepository;
    private readonly ITransferRepository _transferRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITransactionValidator _validator;
    private readonly IReferenceNormalizer _referenceNormalizer;
    private readonly IDailyDebitLimitChecker _limitChecker;
    private readonly ITransferMapper _transferMapper;
    private readonly IClock _clock;

    public TransferService(
        IAccountRepository accountRepository,
        ILedgerRepository ledgerRepository,
        ITransferRepository transferRepository,
        IUnitOfWork unitOfWork,
        ITransactionValidator validator,
        IReferenceNormalizer referenceNormalizer,
        IDailyDebitLimitChecker limitChecker,
        ITransferMapper transferMapper,
        IClock clock)
    {
        _accountRepository = accountRepository;
        _ledgerRepository = ledgerRepository;
        _transferRepository = transferRepository;
        _unitOfWork = unitOfWork;
        _validator = validator;
        _referenceNormalizer = referenceNormalizer;
        _limitChecker = limitChecker;
        _transferMapper = transferMapper;
        _clock = clock;
    }

    public async Task<Result<TransferResponse>> TransferAsync(
        TransferRequest request,
        CancellationToken cancellationToken)
    {
        var validation = _validator.ValidateTransfer(
            request.FromAccountId,
            request.ToAccountId,
            request.Amount,
            request.Narrative,
            request.ExternalReference);
        if (validation is not null)
        {
            return Result<TransferResponse>.Failure(validation.ErrorCode, validation.ErrorMessage);
        }

        var externalReference = _referenceNormalizer.Normalize(request.ExternalReference);

        try
        {
            await _unitOfWork.BeginTransactionAsync(cancellationToken);

            var lockFirstId = request.FromAccountId.CompareTo(request.ToAccountId) <= 0 ? request.FromAccountId : request.ToAccountId;
            var lockSecondId = lockFirstId == request.FromAccountId ? request.ToAccountId : request.FromAccountId;

            var first = await _accountRepository.GetByIdForUpdateAsync(lockFirstId, cancellationToken);
            var second = await _accountRepository.GetByIdForUpdateAsync(lockSecondId, cancellationToken);

            if (first is null || second is null)
            {
                await _unitOfWork.RollbackAsync(cancellationToken);
                return Result<TransferResponse>.Failure(ErrorCodes.NotFound, "One or both accounts were not found.");
            }

            var fromAccount = first.Id == request.FromAccountId ? first : second;
            var toAccount = first.Id == request.ToAccountId ? first : second;

            if (!fromAccount.IsActive || !toAccount.IsActive)
            {
                await _unitOfWork.RollbackAsync(cancellationToken);
                return Result<TransferResponse>.Failure(ErrorCodes.BusinessRule, "Both accounts must be active.");
            }

            var now = _clock.UtcNow;
            if (await _limitChecker.WouldExceedDailyLimitAsync(
                fromAccount.Id,
                fromAccount.DailyDebitLimit,
                request.Amount,
                now,
                cancellationToken))
            {
                await _unitOfWork.RollbackAsync(cancellationToken);
                return Result<TransferResponse>.Failure(ErrorCodes.BusinessRule, "Daily debit limit exceeded.");
            }

            Transfer transfer;
            try
            {
                transfer = Transfer.CreatePending(
                    externalReference,
                    fromAccount,
                    toAccount,
                    request.Amount,
                    request.Narrative.Trim(),
                    now);
            }
            catch (InvalidOperationException ex)
            {
                await _unitOfWork.RollbackAsync(cancellationToken);
                return Result<TransferResponse>.Failure(ErrorCodes.BusinessRule, ex.Message);
            }

            await _transferRepository.AddAsync(transfer, cancellationToken);

            try
            {
                fromAccount.Debit(request.Amount, now);
                toAccount.Credit(request.Amount, now);
                transfer.MarkCompleted(now);
            }
            catch (InvalidOperationException ex)
            {
                await _unitOfWork.RollbackAsync(cancellationToken);
                return Result<TransferResponse>.Failure(ErrorCodes.BusinessRule, ex.Message);
            }

            var debitEntry = LedgerEntry.Create(
                fromAccount.Id,
                $"{externalReference}-DR",
                LedgerEntryType.Debit,
                request.Amount,
                fromAccount.Balance,
                $"Transfer to {toAccount.AccountNumber}: {request.Narrative.Trim()}",
                now);

            var creditEntry = LedgerEntry.Create(
                toAccount.Id,
                $"{externalReference}-CR",
                LedgerEntryType.Credit,
                request.Amount,
                toAccount.Balance,
                $"Transfer from {fromAccount.AccountNumber}: {request.Narrative.Trim()}",
                now);

            await _accountRepository.UpdateAsync(fromAccount, cancellationToken);
            await _accountRepository.UpdateAsync(toAccount, cancellationToken);
            await _ledgerRepository.AddAsync(debitEntry, cancellationToken);
            await _ledgerRepository.AddAsync(creditEntry, cancellationToken);
            await _transferRepository.UpdateAsync(transfer, cancellationToken);

            await _unitOfWork.CommitAsync(cancellationToken);
            return Result<TransferResponse>.Success(_transferMapper.Map(transfer));
        }
        catch (DuplicateResourceException)
        {
            await _unitOfWork.RollbackAsync(cancellationToken);

            var existingTransfer = await _transferRepository.GetByExternalReferenceAsync(externalReference, cancellationToken);
            if (existingTransfer is not null && existingTransfer.Status == TransferStatus.Completed)
            {
                return Result<TransferResponse>.Success(_transferMapper.Map(existingTransfer));
            }

            return Result<TransferResponse>.Failure(
                ErrorCodes.Conflict,
                "A transfer with this external reference already exists.");
        }
        catch
        {
            await _unitOfWork.RollbackAsync(cancellationToken);
            throw;
        }
    }
}
