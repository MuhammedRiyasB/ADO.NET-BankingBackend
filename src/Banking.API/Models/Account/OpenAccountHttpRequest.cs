using System.ComponentModel.DataAnnotations;
using Banking.Domain.Enums;

namespace Banking.API.Models.Account;

public sealed class OpenAccountHttpRequest
{
    [Required]
    public Guid CustomerId { get; init; }

    [Required]
    [EnumDataType(typeof(AccountType))]
    public AccountType AccountType { get; init; }

    [Required]
    [StringLength(3, MinimumLength = 3)]
    public string Currency { get; init; } = string.Empty;

    [Range(typeof(decimal), "1", "100000.99")]
    public decimal DailyDebitLimit { get; init; }
}
