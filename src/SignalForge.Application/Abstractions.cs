using SignalForge.Domain;

namespace SignalForge.Application;

// Repository abstractions (domain-based).
// Use cases map DTOs at the boundary to these domain models.
public interface IRuleRepository
{
    Task AddAsync(Rule rule, CancellationToken cancellationToken);
    Task<IReadOnlyList<Rule>> ListActiveAsync(CancellationToken cancellationToken);
}

public interface ISignalRepository
{
    Task AddAsync(Signal signal, CancellationToken cancellationToken);
    Task<IReadOnlyList<Signal>> ListAsync(CancellationToken cancellationToken);
}

public interface IAlertRepository
{
    Task AddAsync(Alert alert, CancellationToken cancellationToken);
    Task<IReadOnlyList<Alert>> ListAsync(CancellationToken cancellationToken);
}

// Identity/crypto abstractions (technical, not transport-specific).
public interface IUserRepository
{
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken);
    Task<User?> GetByIdAsync(Guid userId, CancellationToken cancellationToken);
    Task AddAsync(User user, CancellationToken cancellationToken);
}

public interface IPasswordHasher
{
    string Hash(string password);
    bool Verify(string password, string passwordHash);
}

public interface ITokenService
{
    string CreateToken(User user);
}

public interface IDateTimeProvider
{
    DateTime UtcNow { get; }
}

