using Banking.Application.Common;
using Banking.Application.DTOs.Account;
using Banking.Application.Interfaces;
using Banking.Application.Mappers;
using Banking.Application.Services.Accounts;
using Banking.Application.Validators;
using Banking.Domain.Entities;

namespace Banking.Application.Services;

/// <summary>
/// Service for account operations (creation, retrieval).
/// Delegates to specialized services for specific concerns.
/// </summary>
public sealed class AccountService : IAccountService
{
    private readonly IAccountRepository _accountRepository;
    private readonly ICustomerRepository _customerRepository;
    private readonly IAccountNumberGenerator _accountNumberGenerator;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrencyValidator _currencyValidator;
    private readonly IAmountValidator _amountValidator;
    private readonly IAccountMapper _accountMapper;
    private readonly IClock _clock;

    public AccountService(
        IAccountRepository accountRepository,
        ICustomerRepository customerRepository,
        IAccountNumberGenerator accountNumberGenerator,
        IUnitOfWork unitOfWork,
        ICurrencyValidator currencyValidator,
        IAmountValidator amountValidator,
        IAccountMapper accountMapper,
        IClock clock)
    {
        _accountRepository = accountRepository;
        _customerRepository = customerRepository;
        _accountNumberGenerator = accountNumberGenerator;
        _unitOfWork = unitOfWork;
        _currencyValidator = currencyValidator;
        _amountValidator = amountValidator;
        _accountMapper = accountMapper;
        _clock = clock;
    }

    public async Task<Result<AccountResponse>> OpenAccountAsync(
        OpenAccountRequest request,
        CancellationToken cancellationToken)
    {
        if (request.CustomerId == Guid.Empty)
        {
            return Result<AccountResponse>.Failure(ErrorCodes.Validation, "CustomerId is required.");
        }

        if (!_currencyValidator.IsValidCurrency(request.Currency))
        {
            return Result<AccountResponse>.Failure(ErrorCodes.Validation, "Currency must be a 3-letter ISO code.");
        }

        if (!_amountValidator.IsValidLimit(request.DailyDebitLimit))
        {
            return Result<AccountResponse>.Failure(ErrorCodes.Validation, "Daily debit limit must be greater than zero.");
        }

        var customer = await _customerRepository.GetByIdAsync(request.CustomerId, cancellationToken);
        if (customer is null)
        {
            return Result<AccountResponse>.Failure(ErrorCodes.NotFound, "Customer not found.");
        }

        if (!customer.IsActive)
        {
            return Result<AccountResponse>.Failure(ErrorCodes.BusinessRule, "Inactive customer cannot open accounts.");
        }

        string accountNumber = string.Empty;
        var uniqueNumberFound = false;
        for (var retry = 0; retry < 10; retry++)
        {
            accountNumber = _accountNumberGenerator.Generate();
            var existing = await _accountRepository.GetByAccountNumberAsync(accountNumber, cancellationToken);
            if (existing is null)
            {
                uniqueNumberFound = true;
                break;
            }
        }

        if (!uniqueNumberFound)
        {
            return Result<AccountResponse>.Failure(ErrorCodes.Conflict, "Unable to generate a unique account number.");
        }

        var account = Account.OpenNew(
            request.CustomerId,
            accountNumber,
            request.AccountType,
            request.Currency,
            request.DailyDebitLimit,
            _clock.UtcNow);

        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        await _accountRepository.AddAsync(account, cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);
        
        return Result<AccountResponse>.Success(_accountMapper.Map(account));
    }

    public async Task<Result<AccountResponse>> GetByIdAsync(Guid accountId, CancellationToken cancellationToken)
    {
        var account = await _accountRepository.GetByIdAsync(accountId, cancellationToken);
        if (account is null)
        {
            return Result<AccountResponse>.Failure(ErrorCodes.NotFound, "Account not found.");
        }

        return Result<AccountResponse>.Success(_accountMapper.Map(account));
    }
}
