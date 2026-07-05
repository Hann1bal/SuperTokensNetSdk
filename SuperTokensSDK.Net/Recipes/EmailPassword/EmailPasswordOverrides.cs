using SuperTokensSDK.Net.Core;
using SuperTokensSDK.Net.Core.Models;

namespace SuperTokensSDK.Net.Recipes.EmailPassword;

/// <summary>
/// Nullable delegate overrides for the EmailPassword recipe.
/// </summary>
public sealed class EmailPasswordOverrides : RecipeOverrides
{
    public Func<string, string, CancellationToken, Task<UserResponse?>>? SignUp { get; set; }
    public Func<string, string, CancellationToken, Task<UserResponse?>>? SignIn { get; set; }
    public Func<string, string, CancellationToken, Task<bool>>? ResetPassword { get; set; }
    public Func<string, CancellationToken, Task<UserResponse?>>? GetUserById { get; set; }
    public Func<string, string, CancellationToken, Task<UserResponse?>>? GetUserByEmail { get; set; }
    public Func<string, CancellationToken, Task<string?>>? GeneratePasswordResetToken { get; set; }
    public Func<string, string?, string?, CancellationToken, Task>? UpdateEmailOrPassword { get; set; }
    public Func<string, string, CancellationToken, Task<bool>>? EmailExists { get; set; }
}
