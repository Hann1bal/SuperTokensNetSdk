using SuperTokensSDK.Net.DataClasses;

namespace SuperTokensSDK.Net
{
    public interface ISuperTokenService
    {
        Task<string> CreateSession(string userId);
        Task<TokenValidationResult> ValidateSession(string AccessToken);
        Task<TokenValidationResult> ValidateRefreshToken(string RefreshToken);
    }
}