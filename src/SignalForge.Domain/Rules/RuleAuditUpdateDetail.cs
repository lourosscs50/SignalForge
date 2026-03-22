namespace SignalForge.Domain;

/// <summary>Structured change detail for <see cref="RuleAuditActions.Updated"/> entries only.</summary>
public sealed record RuleAuditUpdateDetail(
    string PreviousName,
    string NewName,
    string PreviousMatchValue,
    string NewMatchValue);
