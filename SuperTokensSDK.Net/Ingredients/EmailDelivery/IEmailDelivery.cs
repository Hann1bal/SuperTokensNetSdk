namespace SuperTokensSDK.Net.Ingredients.EmailDelivery;

/// <summary>
/// Abstraction for sending transactional emails from SuperTokens recipes.
/// </summary>
public interface IEmailDelivery
{
    Task SendEmailAsync(EmailDeliveryInput input, CancellationToken ct = default);
}

/// <summary>
/// Input describing an email that the SDK wants to deliver.
/// </summary>
public class EmailDeliveryInput
{
    /// <summary>
    /// Email type: PASSWORD_RESET, EMAIL_VERIFICATION or PASSWORDLESS_LOGIN.
    /// </summary>
    public string Type { get; init; } = "";

    public string ToEmail { get; init; } = "";

    public string? Subject { get; set; }

    public string? Body { get; set; }

    public string? Html { get; set; }

    public Dictionary<string, object> TemplateVars { get; init; } = new();
}
