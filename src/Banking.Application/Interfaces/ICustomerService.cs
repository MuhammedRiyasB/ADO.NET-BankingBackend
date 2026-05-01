using Banking.Application.Common;
using Banking.Application.DTOs.Customer;

namespace Banking.Application.Interfaces;

public interface ICustomerService
{
    Task<Result<CustomerResponse>> CreateAsync(CreateCustomerRequest request, CancellationToken cancellationToken);
    Task<Result<CustomerResponse>> GetByIdAsync(Guid customerId, CancellationToken cancellationToken);
}
