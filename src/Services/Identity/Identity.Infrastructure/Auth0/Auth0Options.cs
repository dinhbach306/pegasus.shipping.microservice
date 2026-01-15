namespace Identity.Infrastructure.Auth0;

public sealed class Auth0Options
{
    public string Domain { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public string Connection { get; set; } = string.Empty;
}

