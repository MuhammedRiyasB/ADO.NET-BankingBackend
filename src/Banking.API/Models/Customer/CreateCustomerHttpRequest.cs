using System.ComponentModel.DataAnnotations;

namespace Banking.API.Models.Customer;

public sealed class CreateCustomerHttpRequest
{
    [Required]
    [StringLength(200, MinimumLength = 3)]
    public string FullName { get; init; } = string.Empty;

    [Required]
    [EmailAddress]
    [StringLength(200)]
    public string Email { get; init; } = string.Empty;

    [Required]
    [StringLength(30, MinimumLength = 8)]
    public string PhoneNumber { get; init; } = string.Empty;
}
