namespace SuperTokensSDK.Net.Core.Models;

public sealed class CreateEmailVerificationTokenRequest
{
    public string UserId { get; set; } = "";
    public string? Email { get; set; }
}

public sealed class CreateEmailVerificationTokenResponse
{
    public string? Status { get; set; }
    public string? Token { get; set; }
}

public sealed class VerifyEmailRequest
{
    public string Method { get; set; } = "token";
    public string Token { get; set; } = "";
}

public sealed class VerifyEmailResponse
{
    public string? Status { get; set; }
    public bool EmailVerified { get; set; }
}

public sealed class IsEmailVerifiedRequest
{
    public string UserId { get; set; } = "";
    public string? Email { get; set; }
}

public sealed class IsEmailVerifiedResponse
{
    public string? Status { get; set; }
    public bool IsVerified { get; set; }
}

public sealed class RevokeEmailVerificationTokensRequest
{
    public string UserId { get; set; } = "";
    public string? Email { get; set; }
}

public sealed class UnverifyEmailRequest
{
    public string UserId { get; set; } = "";
    public string? Email { get; set; }
}
