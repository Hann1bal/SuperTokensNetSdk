namespace SuperTokensSDK.Net.Core.Models;

public sealed class CreateTotpDeviceRequest
{
    public string UserId { get; set; } = "";
    public string? DeviceName { get; set; }
    public int Skew { get; set; } = 0;
    public int Period { get; set; } = 30;
}

public sealed class CreateTotpDeviceResponse
{
    public string? Status { get; set; }
    public string? Secret { get; set; }
    public string? DeviceName { get; set; }
}

public sealed class VerifyTotpDeviceRequest
{
    public string UserId { get; set; } = "";
    public string DeviceName { get; set; } = "";
    public string Totp { get; set; } = "";
}

public sealed class VerifyTotpDeviceResponse
{
    public string? Status { get; set; }
    public bool WasAlreadyVerified { get; set; }
    public int CurrentNumberOfFailedAttempts { get; set; }
    public int MaxNumberOfFailedAttempts { get; set; }
    public long? RetryAfterMs { get; set; }
}

public sealed class VerifyTotpCodeRequest
{
    public string UserId { get; set; } = "";
    public string Totp { get; set; } = "";
    public bool AllowUnverifiedDevices { get; set; } = false;
}

public sealed class VerifyTotpCodeResponse
{
    public string? Status { get; set; }
    public int CurrentNumberOfFailedAttempts { get; set; }
    public int MaxNumberOfFailedAttempts { get; set; }
    public long? RetryAfterMs { get; set; }
}

public sealed class TotpDevice
{
    public string Name { get; set; } = "";
    public int Period { get; set; }
    public int Skew { get; set; }
    public bool Verified { get; set; }
}

public sealed class ListTotpDevicesResponse
{
    public string? Status { get; set; }
    public List<TotpDevice> Devices { get; set; } = new();
}

public sealed class RemoveTotpDeviceRequest
{
    public string UserId { get; set; } = "";
    public string DeviceName { get; set; } = "";
}

public sealed class RemoveTotpDeviceResponse
{
    public string? Status { get; set; }
    public bool DidDeviceExist { get; set; }
}
