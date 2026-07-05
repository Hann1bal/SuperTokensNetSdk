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

    public async Task<UserResponse?> GetUserByIdAsync(string userId, CancellationToken cancellationToken = default)
    {
        var response = await _coreApiClient.GetUserByIdAsync(userId, cancellationToken);
        return response.User;
    }

    public async Task<UserResponse?> GetUserByEmailAsync(string email, string tenantId = "public", CancellationToken cancellationToken = default)
    {
        var response = await _coreApiClient.GetUserByEmailAsync(email, tenantId, cancellationToken);
        return response.User;
    }

    public async Task<string?> GeneratePasswordResetTokenAsync(string userId, CancellationToken cancellationToken = default)
    {
        var response = await _coreApiClient.GeneratePasswordResetTokenAsync(
            new GeneratePasswordResetTokenRequest { UserId = userId }, cancellationToken);
        return response.Token;
    }

    public async Task UpdateEmailOrPasswordAsync(string userId, string? newEmail = null, string? newPassword = null, CancellationToken cancellationToken = default)
    {
        await _coreApiClient.UpdateEmailOrPasswordAsync(
            new UpdateEmailOrPasswordRequest { UserId = userId, Email = newEmail, Password = newPassword }, cancellationToken);
    }

    public async Task<bool> EmailExistsAsync(string email, string tenantId = "public", CancellationToken cancellationToken = default)
    {
        var response = await _coreApiClient.EmailExistsAsync(email, tenantId, cancellationToken);
        return response.Exists;
    }
}
