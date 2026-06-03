namespace MyCars.Infrastructure.Email;

public sealed class NullEmailService : IEmailService
{
    private readonly ILogger<NullEmailService> _log;
    public NullEmailService(ILogger<NullEmailService> log) => _log = log;

    public Task SendAsync(string to, string subject, string htmlBody)
    {
        _log.LogWarning("SMTP non configurato — email non inviata. Destinatario: {To} Oggetto: {Subject}", to, subject);
        return Task.CompletedTask;
    }
}
