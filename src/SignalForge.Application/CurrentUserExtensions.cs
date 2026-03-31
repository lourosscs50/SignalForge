namespace SignalForge.Application;

public static class CurrentUserExtensions
{
    /// <exception cref="InvalidOperationException">When no authenticated actor id is available.</exception>
    public static string RequireUserId(this ICurrentUser current)
    {
        var id = current.UserId;
        if (string.IsNullOrWhiteSpace(id))
            throw new InvalidOperationException("Authenticated actor identity is required.");
        return id.Trim();
    }
}
