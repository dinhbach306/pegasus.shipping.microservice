using Identity.Application;
using Microsoft.Extensions.Options;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace Identity.Infrastructure.Email;

public sealed class SendGridEmailSender(IOptions<SendGridOptions> options) : IEmailSender
{
    public async Task SendAsync(string toEmail, string subject, string htmlContent, CancellationToken cancellationToken = default)
    {
        var settings = options.Value;
        var client = new SendGridClient(settings.ApiKey);
        var message = MailHelper.CreateSingleEmail(
            new EmailAddress(settings.FromEmail, settings.FromName),
            new EmailAddress(toEmail),
            subject,
            plainTextContent: string.Empty,
            htmlContent: htmlContent);

        await client.SendEmailAsync(message, cancellationToken);
    }
}

