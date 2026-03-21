using SignalForge.Contracts.Identity;
using SignalForge.Domain;

namespace SignalForge.Application.UseCases.Identity;

public static class RegisterUser
{
    public sealed class Handler(
        IUserRepository users,
        IPasswordHasher passwordHasher,
        ITokenService tokenService,
        IDateTimeProvider clock)
    {
        public async Task<AuthResponse> HandleAsync(
            RegisterRequest request,
            CancellationToken cancellationToken)
        {
            var email = (request.Email ?? string.Empty).Trim().ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(email))
                throw new ArgumentException("Email is required.", nameof(request));

            var displayName = (request.DisplayName ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(displayName))
                throw new ArgumentException("Display name is required.", nameof(request));

            var password = request.Password ?? string.Empty;
            if (string.IsNullOrWhiteSpace(password))
                throw new ArgumentException("Password is required.", nameof(request));

            var existing = await users.GetByEmailAsync(email, cancellationToken);
            if (existing is not null)
                throw new InvalidOperationException("User already exists.");

            var user = new User(
                Id: Guid.NewGuid(),
                Email: email,
                DisplayName: displayName,
                PasswordHash: passwordHasher.Hash(password),
                CreatedAtUtc: clock.UtcNow);

            await users.AddAsync(user, cancellationToken);

            return new AuthResponse(tokenService.CreateToken(user));
        }
    }
}

