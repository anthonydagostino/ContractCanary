using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OppSignal.Application.Email;

namespace OppSignal.Infrastructure.Email;

/// <summary>
/// Development email transport: writes each message to <c>maildrop/</c> as .html +
/// .txt and logs a line. Zero credentials — used by the fixture-mode compose stack.
/// </summary>
public sealed class DevEmailSender : IEmailSender
{
    private readonly EmailOptions _options;
    private readonly ILogger<DevEmailSender> _log;

    public DevEmailSender(IOptions<EmailOptions> options, ILogger<DevEmailSender> log)
    {
        _options = options.Value;
        _log = log;
    }

    public string Provider => "Dev";

    public async Task<EmailSendResult> SendAsync(EmailMessage message, CancellationToken ct = default)
    {
        try
        {
            Directory.CreateDirectory(_options.DevDropPath);
            var safeTo = message.ToAddress.Replace("@", "_at_").Replace("/", "_");
            var id = $"{DateTime.UtcNow:yyyyMMdd-HHmmss}-{message.Kind}-{safeTo}-{Guid.NewGuid():N}".ToLowerInvariant();
            var htmlPath = Path.Combine(_options.DevDropPath, id + ".html");
            var textPath = Path.Combine(_options.DevDropPath, id + ".txt");

            await File.WriteAllTextAsync(htmlPath, message.HtmlBody, ct);
            await File.WriteAllTextAsync(textPath,
                $"To: {message.ToAddress}\nSubject: {message.Subject}\n\n{message.TextBody}", ct);

            _log.LogInformation("[DevEmail] {Kind} -> {To} | {Subject} | saved {Path}",
                message.Kind, message.ToAddress, message.Subject, htmlPath);

            return new EmailSendResult(true, Provider, id, null);
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "DevEmailSender failed for {To}", message.ToAddress);
            return new EmailSendResult(false, Provider, null, ex.Message);
        }
    }
}
