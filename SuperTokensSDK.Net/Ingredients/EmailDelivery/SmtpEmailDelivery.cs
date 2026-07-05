using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;

namespace SuperTokensSDK.Net.Ingredients.EmailDelivery;

/// <summary>
/// Sends emails via an SMTP server using System.Net.Mail.
/// </summary>
public class SmtpEmailDelivery : IEmailDelivery
{
    private readonly SmtpOptions _options;

    public SmtpEmailDelivery(IOptions<SmtpOptions> options)
    {
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
    }

    public async Task SendEmailAsync(EmailDeliveryInput input, CancellationToken ct = default)
    {
        if (input == null)
        {
            throw new ArgumentNullException(nameof(input));
        }

        using var message = new MailMessage();
        message.From = new MailAddress(_options.FromEmail, _options.FromName);
        message.To.Add(input.ToEmail);
        message.Subject = input.Subject ?? "";
        message.Body = input.Html ?? input.Body ?? "";
        message.IsBodyHtml = input.Html != null;

        using var client = new SmtpClient(_options.Host, _options.Port);
        client.Credentials = new NetworkCredential(_options.Username, _options.Password);
        client.EnableSsl = _options.EnableSsl;

        await client.SendMailAsync(message, ct);
    }
}

/// <summary>
/// Configuration options for <see cref="SmtpEmailDelivery"/>.
/// </summary>
public class SmtpOptions
{
    public string Host { get; set; } = "";

    public int Port { get; set; } = 587;

    public string Username { get; set; } = "";

    public string Password { get; set; } = "";

    public bool EnableSsl { get; set; } = true;

    public string FromEmail { get; set; } = "";

    public string FromName { get; set; } = "";
}
