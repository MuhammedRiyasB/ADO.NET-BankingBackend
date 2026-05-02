using System.Net.Mail;
using Banking.Application.Common;
using Banking.Application.DTOs.Customer;
using Banking.Application.Interfaces;
using Banking.Domain.Entities;

namespace Banking.Application.Services;

public sealed class CustomerService : ICustomerService
{
    private readonly ICustomerRepository _customerRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public CustomerService(ICustomerRepository customerRepository, IUnitOfWork unitOfWork, IClock clock)
    {
        _customerRepository = customerRepository;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<Result<CustomerResponse>> CreateAsync(
        CreateCustomerRequest request,
        CancellationToken cancellationToken)
    {
        var fullName = request.FullName?.Trim() ?? string.Empty;
        var email = request.Email?.Trim() ?? string.Empty;
        var phoneNumber = request.PhoneNumber?.Trim() ?? string.Empty;

        if (fullName.Length < 3)
        {
            return Result<CustomerResponse>.Failure(ErrorCodes.Validation, "Full name must have at least 3 characters.");
        }

        if (!IsValidEmail(email))
        {
            return Result<CustomerResponse>.Failure(ErrorCodes.Validation, "Invalid email address.");
        }

        if (phoneNumber.Length < 8)
        {
            return Result<CustomerResponse>.Failure(ErrorCodes.Validation, "Phone number must have at least 8 characters.");
        }

        var customer = Customer.CreateNew(fullName, email, phoneNumber, _clock.UtcNow);

        try
        {
            await _unitOfWork.BeginTransactionAsync(cancellationToken);
            await _customerRepository.AddAsync(customer, cancellationToken);
            await _unitOfWork.CommitAsync(cancellationToken);

            return Result<CustomerResponse>.Success(Map(customer));
        }
        catch (DuplicateResourceException)
        {
            await _unitOfWork.RollbackAsync(cancellationToken);
            return Result<CustomerResponse>.Failure(ErrorCodes.Conflict, "A customer with this email already exists.");
        }
        catch
        {
            await _unitOfWork.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<Result<CustomerResponse>> GetByIdAsync(Guid customerId, CancellationToken cancellationToken)
    {
        var customer = await _customerRepository.GetByIdAsync(customerId, cancellationToken);
        if (customer is null)
        {
            return Result<CustomerResponse>.Failure(ErrorCodes.NotFound, "Customer not found.");
        }

        return Result<CustomerResponse>.Success(Map(customer));
    }

    private static CustomerResponse Map(Customer customer)
    {
        return new CustomerResponse(
            customer.Id,
            customer.FullName,
            customer.Email,
            customer.PhoneNumber,
            customer.KycStatus.ToString(),
            customer.IsActive,
            customer.CreatedAtUtc);
    }

    private static bool IsValidEmail(string email)
    {
        try
        {
            _ = new MailAddress(email);
            return true;
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
