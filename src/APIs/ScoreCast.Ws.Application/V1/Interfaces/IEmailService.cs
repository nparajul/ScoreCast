namespace ScoreCast.Ws.Application.V1.Interfaces;

public interface IEmailService
{
    Task SendWelcomeEmailAsync(string toEmail, string displayName, CancellationToken ct);
}
