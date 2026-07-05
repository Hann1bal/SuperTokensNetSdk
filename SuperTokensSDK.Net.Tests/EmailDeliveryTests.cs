using Microsoft.Extensions.Options;
using SuperTokensSDK.Net.Ingredients.EmailDelivery;

using Xunit;

namespace SuperTokensSDK.Net.Tests;

public class EmailDeliveryTests
{
    [Fact]
    public void Constructor_NullOptions_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new SmtpEmailDelivery(null!));
    }

    [Fact]
    public async Task SendEmailAsync_NullInput_Throws()
    {
        var options = Options.Create(new SmtpOptions { Host = "localhost" });
        var delivery = new SmtpEmailDelivery(options);

        await Assert.ThrowsAsync<ArgumentNullException>(() => delivery.SendEmailAsync(null!));
    }

    [Fact]
    public void EmailDeliveryInput_Defaults_AreSet()
    {
        var input = new EmailDeliveryInput();

        Assert.Equal(string.Empty, input.Type);
        Assert.Equal(string.Empty, input.ToEmail);
        Assert.NotNull(input.TemplateVars);
        Assert.Empty(input.TemplateVars);
    }

    [Fact]
    public void SmtpOptions_Defaults_AreSet()
    {
        var options = new SmtpOptions();

        Assert.Equal(string.Empty, options.Host);
        Assert.Equal(587, options.Port);
        Assert.Equal(SmtpSecureMode.Auto, options.Secure);
        Assert.Null(options.TlsProtocols);
        Assert.Equal(string.Empty, options.FromEmail);
    }

    [Fact]
    public void SmtpOptions_SecureMode_CanBeSet()
    {
        var options = new SmtpOptions { Secure = SmtpSecureMode.ImplicitSsl, Port = 465 };

        Assert.Equal(SmtpSecureMode.ImplicitSsl, options.Secure);
        Assert.Equal(465, options.Port);
    }
}
