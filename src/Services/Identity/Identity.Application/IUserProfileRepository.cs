using Identity.Domain;

namespace Identity.Application;

public interface IUserProfileRepository
{
    Task<UserProfile?> GetByAuth0IdAsync(string auth0UserId, CancellationToken cancellationToken = default);
    Task AddAsync(UserProfile profile, CancellationToken cancellationToken = default);
}

