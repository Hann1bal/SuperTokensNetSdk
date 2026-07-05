namespace SuperTokensSDK.Net.Ingredients.SmsDelivery;

/// <summary>
/// Abstraction for sending transactional SMS messages from SuperTokens recipes.
/// </summary>
public interface ISmsDelivery
{
    Task SendSmsAsync(SmsDeliveryInput input, CancellationToken ct = default);
}

/// <summary>
/// Input describing an SMS that the SDK wants to deliver.
/// </summary>
public class SmsDeliveryInput
{
    /// <summary>
    /// SMS type: PASSWORDLESS_LOGIN.
    /// </summary>
    public string Type { get; init; } = "";

    public string ToPhoneNumber { get; init; } = "";

    public string? Body { get; set; }

    public Dictionary<string, object> TemplateVars { get; init; } = new();
}
