namespace Banking.Application.DTOs.Customer;

public sealed record CreateCustomerRequest(
    string FullName,
    string Email,
    string PhoneNumber);
