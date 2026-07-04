namespace SuperTokensSDK.Net.Core;

/// <summary>
/// Base exception for all SuperTokens SDK errors.
/// </summary>
public class SuperTokensException : Exception
{
    public SuperTokensException(string message) : base(message) { }

    public SuperTokensException(string message, Exception innerException) : base(message, innerException) { }
}
