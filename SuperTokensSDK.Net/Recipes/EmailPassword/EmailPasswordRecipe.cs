using SuperTokensSDK.Net.Core;
using SuperTokensSDK.Net.Core.Models;

namespace SuperTokensSDK.Net.Recipes.EmailPassword;

/// <summary>
/// SuperTokens EmailPassword recipe operations.
/// </summary>
public class EmailPasswordRecipe
{
    private readonly ICoreApiClient _coreApiClient;

    public EmailPasswordRecipe(ICoreApiClient coreApiClient)
    {
        _coreApiClient = coreApiClient ?? throw new ArgumentNullException(nameof(coreApiClient));
    }

    public async Task<UserResponse?> SignUpAsync(string email, string password, CancellationToken cancellationToken = default)
    {
        var response = await _coreApiClient.SignUpAsync(new SignUpRequest { Email = email, Password = password }, cancellationToken);
        return response.User;
    }

    public async Task<UserResponse?> SignInAsync(string email, string password, CancellationToken cancellationToken = default)
    {
        var response = await _coreApiClient.SignInAsync(new SignUpRequest { Email = email, Password = password }, cancellationToken);
        return response.User;
    }

    public async Task ResetPasswordAsync(string userId, string newPassword, CancellationToken cancellationToken = default)
    {
        await _coreApiClient.ResetPasswordAsync(new PasswordResetRequest { UserId = userId, NewPassword = newPassword }, cancellationToken);
    }
}
