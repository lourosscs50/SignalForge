using SignalForge.Application.Queries;
using SignalForge.Contracts;
using SignalForge.Domain;

namespace SignalForge.Application;

// Repository abstractions (domain-based).
// Use cases map DTOs at the boundary to these domain models.
public interface IRuleRepository
{
    Task AddAsync(Rule rule, CancellationToken cancellationToken);
    Task<Rule?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task UpdateAsync(Rule rule, CancellationToken cancellationToken);
    Task<IReadOnlyList<Rule>> ListActiveAsync(CancellationToken cancellationToken);
    Task<PagedResult<Rule>> ListPagedAsync(RuleListQuery query, CancellationToken cancellationToken);
}

public interface IRuleAuditRepository
{
    Task AddAsync(RuleAuditEntry entry, CancellationToken cancellationToken);
    Task<IReadOnlyList<RuleAuditEntry>> ListByRuleIdAsync(Guid ruleId, CancellationToken cancellationToken);
}

public interface ISignalRepository
{
    Task AddAsync(Signal signal, CancellationToken cancellationToken);
    Task<Signal?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<PagedResult<Signal>> ListPagedAsync(SignalListQuery query, CancellationToken cancellationToken);
}

public interface IAlertRepository
{
    Task AddAsync(Alert alert, CancellationToken cancellationToken);
    Task<Alert?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task UpdateAsync(Alert alert, CancellationToken cancellationToken);
    Task<PagedResult<Alert>> ListPagedAsync(AlertListQuery query, CancellationToken cancellationToken);
    Task<IReadOnlyList<Alert>> ListAllAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<Alert>> ListByRuleIdAsync(Guid ruleId, CancellationToken cancellationToken);
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

/// <summary>Authenticated actor for lifecycle mutations (transport-agnostic).</summary>
public interface ICurrentUser
{
    /// <summary>Stable user identifier (e.g. JWT sub). Null when unavailable or unauthenticated.</summary>
    string? UserId { get; }
}

/// <summary>Strategy for evaluating a single rule style against a signal.</summary>
public interface IRuleEvaluator
{
    bool CanEvaluate(Rule rule);
    bool IsMatch(Rule rule, Signal signal);
}

/// <summary>Orchestrates rule evaluation after signal ingestion.</summary>
public interface ISignalEvaluationService
{
    Task EvaluateAsync(Signal signal, CancellationToken cancellationToken = default);
}

