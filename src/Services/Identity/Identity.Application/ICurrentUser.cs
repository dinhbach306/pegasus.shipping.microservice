namespace Identity.Application;

public interface ICurrentUser
{
    string? UserId { get; }
    string? Email { get; }
}

