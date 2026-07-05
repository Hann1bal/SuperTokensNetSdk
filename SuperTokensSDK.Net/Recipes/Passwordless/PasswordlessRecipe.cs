using PhoneNumbers;
using SuperTokensSDK.Net.Core;
using SuperTokensSDK.Net.Core.Models;

namespace SuperTokensSDK.Net.Recipes.Passwordless;

/// <summary>
/// SuperTokens Passwordless recipe: creates and consumes email/phone sign-in
/// codes and normalizes phone numbers via libphonenumber.
/// </summary>
public class PasswordlessRecipe : IOverridableRecipe
{
    private readonly ICoreApiClient _coreApiClient;
    private static readonly PhoneNumberUtil _phoneUtil = PhoneNumberUtil.GetInstance();

    public PasswordlessRecipe(ICoreApiClient coreApiClient)
    {
        _coreApiClient = coreApiClient ?? throw new ArgumentNullException(nameof(coreApiClient));
    }

    public PasswordlessOverrides? Overrides { get; set; }

    RecipeOverrides? IOverridableRecipe.Overrides
    {
        get => Overrides;
        set => Overrides = (PasswordlessOverrides?)value;
    }

    /// <summary>
    /// Validates a phone number and normalizes it to E.164 format.
    /// Uses the override delegate if set, otherwise falls back to libphonenumber.
    /// Returns the normalized phone number, or throws if invalid.
    /// </summary>
    public string? ValidateAndNormalizePhoneNumber(string? phoneNumber, string tenantId = "public")
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
            return null;

        // Use custom validator if provided
        if (Overrides?.ValidatePhoneNumber != null)
        {
            var error = Overrides.ValidatePhoneNumber(phoneNumber, tenantId);
            if (error != null)
                throw new SuperTokensException($"Phone number validation failed: {error}");
            // Still normalize to E.164 if possible
            return NormalizeToE164(phoneNumber);
        }

        // Default: use libphonenumber
        try
        {
            var parsed = _phoneUtil.Parse(phoneNumber, null);
            if (!_phoneUtil.IsValidNumber(parsed))
                throw new SuperTokensException("Phone number is invalid");
            return _phoneUtil.Format(parsed, PhoneNumberFormat.E164);
        }
        catch (NumberParseException ex)
        {
            throw new SuperTokensException($"Phone number is invalid: {ex.Message}");
        }
    }

    /// <summary>
    /// Attempts to format a phone number to E.164 without validation.
    /// Used as a fallback when a custom validator has already approved the number.
    /// </summary>
    private string? NormalizeToE164(string? phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
            return null;

        try
        {
            var parsed = _phoneUtil.Parse(phoneNumber, null);
            return _phoneUtil.Format(parsed, PhoneNumberFormat.E164);
        }
        catch
        {
            // Custom validator said it's valid but libphonenumber can't parse it.
            // Return the trimmed input as-is.
            return phoneNumber.Trim();
        }
    }

    public async Task<(string deviceId, string preAuthSessionId, string linkCode)> CreateCodeAsync(
        string? email = null, string? phoneNumber = null, string? deviceId = null,
        string tenantId = "public", CancellationToken ct = default)
    {
        if (Overrides?.CreateCode != null)
            return await Overrides.CreateCode(email, phoneNumber, deviceId, tenantId, ct);

        // Validate and normalize phone number before sending to Core
        phoneNumber = ValidateAndNormalizePhoneNumber(phoneNumber, tenantId);

        var response = await _coreApiClient.CreatePasswordlessCodeAsync(
            new CreateCodeRequest { Email = email, PhoneNumber = phoneNumber, DeviceId = deviceId }, tenantId, ct);
        if (response.Status != Constants.Status.Ok)
            throw new SuperTokensException($"Failed to create passwordless code: {response.Status}");
        return (response.DeviceId ?? "", response.PreAuthSessionId ?? "", response.LinkCode ?? "");
    }

    public async Task<PasswordlessUser> ConsumeCodeAsync(
        string preAuthSessionId, string? linkCode = null, string? deviceId = null,
        string? userInputCode = null, string tenantId = "public", CancellationToken ct = default)
    {
        if (Overrides?.ConsumeCode != null)
            return await Overrides.ConsumeCode(preAuthSessionId, linkCode, deviceId, userInputCode, tenantId, ct);

        var response = await _coreApiClient.ConsumePasswordlessCodeAsync(
            new ConsumeCodeRequest { PreAuthSessionId = preAuthSessionId, LinkCode = linkCode, DeviceId = deviceId, UserInputCode = userInputCode }, tenantId, ct);
        if (response.Status != Constants.Status.Ok)
            throw new SuperTokensException($"Failed to consume passwordless code: {response.Status}");
        return response.User ?? throw new SuperTokensException("ConsumeCode returned OK but no user");
    }
}
