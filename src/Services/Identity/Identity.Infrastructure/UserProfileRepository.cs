using Identity.Application;
using Identity.Domain;
using Microsoft.EntityFrameworkCore;

namespace Identity.Infrastructure;

public sealed class UserProfileRepository(IdentityDbContext dbContext) : IUserProfileRepository
{
    public Task<UserProfile?> GetByAuth0IdAsync(string auth0UserId, CancellationToken cancellationToken = default)
    {
        return dbContext.UserProfiles.FirstOrDefaultAsync(profile => profile.Auth0UserId == auth0UserId, cancellationToken);
    }

    public async Task AddAsync(UserProfile profile, CancellationToken cancellationToken = default)
    {
        await dbContext.UserProfiles.AddAsync(profile, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}

