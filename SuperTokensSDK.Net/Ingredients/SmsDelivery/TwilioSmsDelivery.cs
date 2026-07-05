using System.Net.Http.Headers;
using System.Text;
using Microsoft.Extensions.Options;

namespace SuperTokensSDK.Net.Ingredients.SmsDelivery;

/// <summary>
/// Sends SMS messages via the Twilio REST API using a plain HttpClient.
/// </summary>
public class TwilioSmsDelivery : ISmsDelivery
{
    private readonly TwilioOptions _options;

    public TwilioSmsDelivery(IOptions<TwilioOptions> options)
    {
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
    }

    public async Task SendSmsAsync(SmsDeliveryInput input, CancellationToken ct = default)
    {
        if (input == null)
        {
            throw new ArgumentNullException(nameof(input));
        }

        using var client = new HttpClient();
        var auth = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{_options.AccountSid}:{_options.AuthToken}"));

        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["To"] = input.ToPhoneNumber,
            ["From"] = _options.FromNumber,
            ["Body"] = input.Body ?? ""
        });

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", auth);

        await client.PostAsync(
            $"https://api.twilio.com/2010-04-01/Accounts/{_options.AccountSid}/Messages.json",
            content,
            ct);
    }
}

/// <summary>
/// Configuration options for <see cref="TwilioSmsDelivery"/>.
/// </summary>
public class TwilioOptions
{
    public string AccountSid { get; set; } = "";

    public string AuthToken { get; set; } = "";

    public string FromNumber { get; set; } = "";
}
