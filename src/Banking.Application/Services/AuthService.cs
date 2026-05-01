using Banking.Application.Common;
using Banking.Application.DTOs.Auth;
using Banking.Application.Interfaces;

namespace Banking.Application.Services;

public sealed class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IClock _clock;

    public AuthService(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        IJwtTokenService jwtTokenService,
        IClock clock)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _jwtTokenService = jwtTokenService;
        _clock = clock;
    }

    public async Task<Result<AuthResponse>> LoginAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
        {
            return Result<AuthResponse>.Failure(ErrorCodes.Validation, "Username and password are required.");
        }

        var user = await _userRepository.GetByUsernameAsync(request.Username.Trim(), cancellationToken);
        if (user is null || !user.IsActive)
        {
            return Result<AuthResponse>.Failure(ErrorCodes.Unauthorized, "Invalid username or password.");
        }

        var isPasswordValid = _passwordHasher.VerifyPassword(
            request.Password,
            user.PasswordHash,
            user.PasswordSalt,
            user.PasswordIterations);

        if (!isPasswordValid)
        {
            return Result<AuthResponse>.Failure(ErrorCodes.Unauthorized, "Invalid username or password.");
        }

        var token = _jwtTokenService.GenerateToken(user, _clock.UtcNow);
        return Result<AuthResponse>.Success(
            new AuthResponse(
                token.Token,
                token.ExpiresAtUtc,
                user.Username,
                user.Role.ToString()));
    }
}
