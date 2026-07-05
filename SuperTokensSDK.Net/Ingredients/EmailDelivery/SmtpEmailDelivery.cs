using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

namespace SuperTokensSDK.Net.Ingredients.EmailDelivery;

/// <summary>
/// Sends emails via an SMTP server using MailKit.
/// Supports implicit SSL (port 465), STARTTLS (port 587), TLS protocol override, and MIME multipart.
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
            throw new ArgumentNullException(nameof(input));

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_options.FromName, _options.FromEmail));
        message.To.Add(MailboxAddress.Parse(input.ToEmail));
        message.Subject = input.Subject ?? "";

        // Build MIME multipart: include both text and HTML if both are provided
        var bodyBuilder = new BodyBuilder();
        if (!string.IsNullOrEmpty(input.Html))
        {
            bodyBuilder.HtmlBody = input.Html;
            if (!string.IsNullOrEmpty(input.Body))
                bodyBuilder.TextBody = input.Body; // MIME multipart: text + HTML
        }
        else if (!string.IsNullOrEmpty(input.Body))
        {
            bodyBuilder.TextBody = input.Body;
        }
        message.Body = bodyBuilder.ToMessageBody();

        using var client = new SmtpClient();

        // Determine secure socket options based on configuration
        var socketOptions = _options.Secure switch
        {
            SmtpSecureMode.ImplicitSsl => SecureSocketOptions.SslOnConnect,        // port 465
            SmtpSecureMode.StartTls => SecureSocketOptions.StartTlsWhenAvailable,  // port 587
            SmtpSecureMode.None => SecureSocketOptions.None,                       // plain
            _ => SecureSocketOptions.Auto,                                         // auto-detect
        };

        // Apply TLS protocol override if specified
        if (_options.TlsProtocols.HasValue)
            client.SslProtocols = _options.TlsProtocols.Value;

        await client.ConnectAsync(_options.Host, _options.Port, socketOptions, ct);

        // Authenticate (username defaults to FromEmail if not specified)
        var username = string.IsNullOrWhiteSpace(_options.Username) ? _options.FromEmail : _options.Username;
        if (!string.IsNullOrWhiteSpace(_options.Password))
            await client.AuthenticateAsync(username, _options.Password, ct);

        await client.SendAsync(message, ct);
        await client.DisconnectAsync(true, ct);
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

    /// <summary>
    /// Secure socket mode. Default is Auto (MailKit auto-detects based on port).
    /// Use ImplicitSsl for port 465, StartTls for port 587, None for plain.
    /// </summary>
    public SmtpSecureMode Secure { get; set; } = SmtpSecureMode.Auto;

    /// <summary>
    /// Override TLS protocols (e.g. SslProtocols.Tls12). Leave null for OS default.
    /// </summary>
    public System.Security.Authentication.SslProtocols? TlsProtocols { get; set; }

    public string FromEmail { get; set; } = "";

    public string FromName { get; set; } = "";
}

/// <summary>
/// SMTP secure socket modes.
/// </summary>
public enum SmtpSecureMode
{
    /// <summary>MailKit auto-detects based on port and server response.</summary>
    Auto,

    /// <summary>Implicit SSL/TLS on connect (port 465). The connection is encrypted from the start.</summary>
    ImplicitSsl,

    /// <summary>STARTTLS upgrade after plain connection (port 587 or 25).</summary>
    StartTls,

    /// <summary>No encryption (not recommended, port 25).</summary>
    None,
}
