using System.ComponentModel.DataAnnotations;

namespace Banking.API.Models.Auth;

public sealed class LoginHttpRequest
{
    [Required]
    [StringLength(100, MinimumLength = 3)]
    public string Username { get; init; } = string.Empty;

    [Required]
    [StringLength(200, MinimumLength = 8)]
    public string Password { get; init; } = string.Empty;
}
