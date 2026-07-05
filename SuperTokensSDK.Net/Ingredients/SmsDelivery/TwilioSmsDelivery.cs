using System.Net.Http.Headers;
using System.Text;
using Microsoft.Extensions.Options;
using SuperTokensSDK.Net.Core;

namespace SuperTokensSDK.Net.Ingredients.SmsDelivery;

/// <summary>
/// Sends SMS messages via the Twilio REST API.
/// Uses IHttpClientFactory for connection pooling (avoids socket exhaustion).
/// Supports both From number and MessagingServiceSid.
/// </summary>
public class TwilioSmsDelivery : ISmsDelivery
{
    private readonly TwilioOptions _options;
    private readonly IHttpClientFactory _httpClientFactory;

    public TwilioSmsDelivery(IOptions<TwilioOptions> options, IHttpClientFactory httpClientFactory)
    {
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));

        // Validate: exactly one of From or MessagingServiceSid must be set
        var hasFrom = !string.IsNullOrWhiteSpace(_options.From);
        var hasService = !string.IsNullOrWhiteSpace(_options.MessagingServiceSid);

        if (!hasFrom && !hasService)
            throw new InvalidOperationException("TwilioOptions: either 'From' or 'MessagingServiceSid' must be set");
        if (hasFrom && hasService)
            throw new InvalidOperationException("TwilioOptions: only one of 'From' or 'MessagingServiceSid' must be set");
    }

    public async Task SendSmsAsync(SmsDeliveryInput input, CancellationToken ct = default)
    {
        if (input == null)
            throw new ArgumentNullException(nameof(input));

        var client = _httpClientFactory.CreateClient(nameof(TwilioSmsDelivery));
        var auth = Convert.ToBase64String(
            Encoding.ASCII.GetBytes($"{_options.AccountSid}:{_options.AuthToken}"));

        var formData = new Dictionary<string, string>
        {
            ["To"] = input.ToPhoneNumber,
            ["Body"] = input.Body ?? ""
        };

        // Send From OR MessagingServiceSid (validated in constructor that exactly one is set)
        if (!string.IsNullOrWhiteSpace(_options.From))
            formData["From"] = _options.From;
        else
            formData["MessagingServiceSid"] = _options.MessagingServiceSid!;

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", auth);

        var response = await client.PostAsync(
            $"https://api.twilio.com/2010-04-01/Accounts/{_options.AccountSid}/Messages.json",
            new FormUrlEncodedContent(formData),
            ct);

        // Check for errors (improvement over reference which silently ignores)
        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(ct);
            throw new SuperTokensException(
                $"Twilio SMS delivery failed: HTTP {(int)response.StatusCode} {response.StatusCode}. {errorBody}");
        }
    }
}

/// <summary>
/// Configuration options for <see cref="TwilioSmsDelivery"/>.
/// Set either 'From' (phone number) or 'MessagingServiceSid' (messaging service), not both.
/// </summary>
public class TwilioOptions
{
    public string AccountSid { get; set; } = "";

    public string AuthToken { get; set; } = "";

    /// <summary>
    /// Twilio phone number to send from (e.g. "+14155551234").
    /// Either this or MessagingServiceSid must be set, but not both.
    /// </summary>
    public string From { get; set; } = "";

    /// <summary>
    /// Twilio Messaging Service SID (e.g. "MGxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx").
    /// Enables sticky sender, geo-match, number pool, and compliance routing.
    /// Either this or From must be set, but not both.
    /// </summary>
    public string MessagingServiceSid { get; set; } = "";
}
