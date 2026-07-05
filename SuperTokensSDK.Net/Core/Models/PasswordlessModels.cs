namespace SuperTokensSDK.Net.Core.Models;

public sealed class CreateCodeRequest
{
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
    public string? DeviceId { get; set; }
}

public sealed class CreateCodeResponse
{
    public string? Status { get; set; }
    public string? PreAuthSessionId { get; set; }
    public string? CodeId { get; set; }
    public string? DeviceId { get; set; }
    public string? UserInputCode { get; set; }
    public string? LinkCode { get; set; }
    public long TimeCreated { get; set; }
    public long CodeLifetime { get; set; }
}

public sealed class ConsumeCodeRequest
{
    public string PreAuthSessionId { get; set; } = "";
    public string? LinkCode { get; set; }
    public string? DeviceId { get; set; }
    public string? UserInputCode { get; set; }
}

public sealed class PasswordlessUser
{
    public string Id { get; set; } = "";
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
    public long TimeJoined { get; set; }
    public List<string> TenantIds { get; set; } = new();
}

public sealed class ConsumeCodeResponse
{
    public string? Status { get; set; }
    public bool CreatedNewUser { get; set; }
    public PasswordlessUser? User { get; set; }
    public string? RecipeUserId { get; set; }
    public PasswordlessDevice? ConsumedDevice { get; set; }
}

public sealed class PasswordlessDevice
{
    public string? PreAuthSessionId { get; set; }
    public string? RecipeUserId { get; set; }
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
    public List<string> TenantIds { get; set; } = new();
    public List<string> FailedAttemptCount { get; set; } = new();
}
