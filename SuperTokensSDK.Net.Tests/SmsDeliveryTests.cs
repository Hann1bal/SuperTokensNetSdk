using Microsoft.Extensions.Options;
using SuperTokensSDK.Net.Ingredients.SmsDelivery;

using Xunit;

namespace SuperTokensSDK.Net.Tests;

public class SmsDeliveryTests
{
    [Fact]
    public void Constructor_NullOptions_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new TwilioSmsDelivery(null!));
    }

    [Fact]
    public async Task SendSmsAsync_NullInput_Throws()
    {
        var options = Options.Create(new TwilioOptions { AccountSid = "sid" });
        var delivery = new TwilioSmsDelivery(options);

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
        Assert.Equal(string.Empty, options.FromNumber);
    }
}
