using System.ComponentModel.DataAnnotations;

namespace Banking.API.Models.Transaction;

public sealed class DepositHttpRequest
{
    [Required]
    public Guid AccountId { get; init; }

    [Range(typeof(decimal), "1", "10000000.99")]
    public decimal Amount { get; init; }

    [Required]
    [StringLength(300, MinimumLength = 2)]
    public string Narrative { get; init; } = string.Empty;

    [Required]
    [StringLength(80, MinimumLength = 3)]
    public string Reference { get; init; } = string.Empty;
}
