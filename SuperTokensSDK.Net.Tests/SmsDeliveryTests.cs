using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SuperTokensSDK.Net.Ingredients.SmsDelivery;

using Xunit;

namespace SuperTokensSDK.Net.Tests;

public class SmsDeliveryTests
{
    private static IHttpClientFactory CreateHttpClientFactory()
    {
        var services = new ServiceCollection();
        services.AddHttpClient();
        return services.BuildServiceProvider().GetRequiredService<IHttpClientFactory>();
    }

    [Fact]
    public void Constructor_NullOptions_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new TwilioSmsDelivery(null!, CreateHttpClientFactory()));
    }

    [Fact]
    public void Constructor_NullHttpClientFactory_Throws()
    {
        var options = Options.Create(new TwilioOptions { From = "+10000000000" });
        Assert.Throws<ArgumentNullException>(() => new TwilioSmsDelivery(options, null!));
    }

    [Fact]
    public void Constructor_NoFromAndNoMessagingServiceSid_Throws()
    {
        var options = Options.Create(new TwilioOptions { AccountSid = "sid" });
        Assert.Throws<InvalidOperationException>(() => new TwilioSmsDelivery(options, CreateHttpClientFactory()));
    }

    [Fact]
    public void Constructor_BothFromAndMessagingServiceSid_Throws()
    {
        var options = Options.Create(new TwilioOptions
        {
            From = "+10000000000",
            MessagingServiceSid = "MGxxx"
        });
        Assert.Throws<InvalidOperationException>(() => new TwilioSmsDelivery(options, CreateHttpClientFactory()));
    }

    [Fact]
    public async Task SendSmsAsync_NullInput_Throws()
    {
        var options = Options.Create(new TwilioOptions { From = "+10000000000" });
        var delivery = new TwilioSmsDelivery(options, CreateHttpClientFactory());

        await Assert.ThrowsAsync<ArgumentNullException>(() => delivery.SendSmsAsync(null!));
    }

    [Fact]
    public void SmsDeliveryInput_Defaults_AreSet()
    {
        var input = new SmsDeliveryInput();

        Assert.Equal(string.Empty, input.Type);
        Assert.Equal(string.Empty, input.ToPhoneNumber);
        Assert.NotNull(input.TemplateVars);
        Assert.Empty(input.TemplateVars);
    }

    [Fact]
    public void TwilioOptions_Defaults_AreSet()
    {
        var options = new TwilioOptions();

        Assert.Equal(string.Empty, options.AccountSid);
        Assert.Equal(string.Empty, options.AuthToken);
        Assert.Equal(string.Empty, options.From);
        Assert.Equal(string.Empty, options.MessagingServiceSid);
    }
}
