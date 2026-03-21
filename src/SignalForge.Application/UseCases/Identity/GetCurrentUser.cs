using SignalForge.Contracts.Identity;
using SignalForge.Domain;

namespace SignalForge.Application.UseCases.Identity;

public static class GetCurrentUser
{
    public sealed record Request(Guid UserId);

    public sealed class Handler(IUserRepository users)
    {
        public async Task<MeResponse> HandleAsync(Request request, CancellationToken cancellationToken)
        {
            var user = await users.GetByIdAsync(request.UserId, cancellationToken)
                ?? throw new InvalidOperationException("User not found.");

            return new MeResponse(user.Id, user.Email, user.DisplayName);
        }
    }
}

