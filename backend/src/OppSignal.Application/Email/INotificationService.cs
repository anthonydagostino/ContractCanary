namespace OppSignal.Application.Email;

/// <summary>
/// High-level, templated notifications. Renders responsive HTML (Razor) and
/// dispatches via <see cref="IEmailSender"/>, logging each send to EmailLog.
/// </summary>
public interface INotificationService
{
    Task SendEmailVerificationAsync(Guid userId, string email, string? name, string verifyUrl, CancellationToken ct = default);
    Task SendPasswordResetAsync(Guid userId, string email, string? name, string resetUrl, CancellationToken ct = default);
    Task SendWelcomeAsync(Guid userId, string email, string? name, CancellationToken ct = default);

    /// <summary>Send a rendered digest. Returns false if there was nothing to send.</summary>
    Task<bool> SendDigestAsync(DigestModel model, CancellationToken ct = default);
}
