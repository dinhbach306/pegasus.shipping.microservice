namespace Identity.Application;

public interface IEmailSender
{
    Task SendAsync(string toEmail, string subject, string htmlContent, CancellationToken cancellationToken = default);
}

