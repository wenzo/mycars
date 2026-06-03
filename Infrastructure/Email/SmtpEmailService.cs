using System.Net;
using System.Net.Mail;

namespace MyCars.Infrastructure.Email;

public sealed class SmtpEmailService : IEmailService
{
    private readonly SmtpOptions _opts;

    public SmtpEmailService(IOptions<SmtpOptions> opts) => _opts = opts.Value;

    public async Task SendAsync(string to, string subject, string htmlBody)
    {
        using var client = new SmtpClient(_opts.Host, _opts.Port)
        {
            EnableSsl   = _opts.UseSsl,
            Credentials = new NetworkCredential(_opts.Username, _opts.Password),
        };

        using var msg = new MailMessage
        {
            From       = new MailAddress(_opts.FromEmail, _opts.FromName),
            Subject    = subject,
            Body       = htmlBody,
            IsBodyHtml = true,
        };
        msg.To.Add(to);

        await client.SendMailAsync(msg);
    }
}
