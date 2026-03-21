using SignalForge.Contracts.Identity;
using SignalForge.Domain;

namespace SignalForge.Application.UseCases.Identity;

public static class LoginUser
{
    public sealed class Handler(
        IUserRepository users,
        IPasswordHasher passwordHasher,
        ITokenService tokenService)
    {
        public async Task<AuthResponse> HandleAsync(
            LoginRequest request,
            CancellationToken cancellationToken)
        {
            var email = (request.Email ?? string.Empty).Trim().ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(email))
                throw new ArgumentException("Email is required.", nameof(request));

            var password = request.Password ?? string.Empty;
            if (string.IsNullOrWhiteSpace(password))
                throw new ArgumentException("Password is required.", nameof(request));

            var user = await users.GetByEmailAsync(email, cancellationToken)
                ?? throw new InvalidOperationException("Invalid credentials.");

            if (!passwordHasher.Verify(password, user.PasswordHash))
                throw new InvalidOperationException("Invalid credentials.");

            return new AuthResponse(tokenService.CreateToken(user));
        }
    }
}

