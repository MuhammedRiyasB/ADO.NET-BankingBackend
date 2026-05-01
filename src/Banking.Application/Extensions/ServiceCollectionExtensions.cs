using Banking.Application.Interfaces;
using Banking.Application.Mappers;
using Banking.Application.Services;
using Banking.Application.Services.Accounts;
using Banking.Application.Services.Transactions;
using Banking.Application.Utilities;
using Banking.Application.Validators;
using Microsoft.Extensions.DependencyInjection;

namespace Banking.Application.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Validators
        services.AddScoped<IAmountValidator, AmountValidator>();
        services.AddScoped<ICurrencyValidator, CurrencyValidator>();
        services.AddScoped<ITransactionValidator, TransactionValidator>();

        // Utilities
        services.AddScoped<IReferenceNormalizer, ReferenceNormalizer>();
        services.AddScoped<IDateRangeHelper, DateRangeHelper>();
        services.AddScoped<IDailyDebitLimitChecker, DailyDebitLimitChecker>();

        // Mappers
        services.AddScoped<IAccountMapper, AccountMapper>();
        services.AddScoped<ITransactionMapper, TransactionMapper>();
        services.AddScoped<ITransferMapper, TransferMapper>();

        // Transaction Services
        services.AddScoped<IDepositService, DepositService>();
        services.AddScoped<IWithdrawService, WithdrawService>();
        services.AddScoped<ITransferService, TransferService>();

        // Account Services
        services.AddScoped<IStatementService, StatementService>();

        // Application Services
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<ICustomerService, CustomerService>();
        services.AddScoped<IAccountService, AccountService>();


        return services;
    }
}

