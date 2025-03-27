using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SuperTokensSDK.Net.DataClasses;

/// <summary>
/// Represents the result of a token validation operation.
/// </summary>
public class TokenValidationResult
{
   /// <summary>
   /// Gets or sets a value indicating whether the token is valid.
   /// </summary>
   public bool IsValid { get; set; }

   /// <summary>
   /// Gets or sets the error message if the token is not valid.
   /// </summary>
   public string? ErrorMessage { get; set; }

   /// <summary>
   /// Gets or sets the user ID associated with the token.
   /// </summary>
   public string? UserId { get; set; }

   /// <summary>
   /// Gets or sets the claims associated with the token.
   /// </summary>
   public Dictionary<string, object> Claims { get; set; } = new();

   /// <summary>
   /// Creates a successful token validation result.
   /// </summary>
   /// <param name="userId">The user ID associated with the token.</param>
   /// <param name="claims">The claims associated with the token.</param>
   /// <returns>A successful token validation result.</returns>
   public static TokenValidationResult Success(string userId, Dictionary<string, object> claims)
       => new() { IsValid = true, UserId = userId, Claims = claims };

   /// <summary>
   /// Creates a failed token validation result.
   /// </summary>
   /// <param name="error">The error message.</param>
   /// <returns>A failed token validation result.</returns>
   public static TokenValidationResult Failed(string error)
       => new() { IsValid = false, ErrorMessage = error };
}