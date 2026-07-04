using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SuperTokensSDK.Net.Recipes.EmailPassword;
using SuperTokensSDK.Net.Recipes.Session;
using SuperTokensSDK.Net.Recipes.UserMetadata;
using SuperTokensSDK.Net.Recipes.UserRoles;

namespace SuperTokensSDK.Net.Endpoints;

/// <summary>
/// Frontend Driver Interface (FDI) endpoints for authentication.
/// </summary>
[ApiController]
[Route("auth")]
public class AuthController : ControllerBase
{
    private readonly SessionRecipe _sessionRecipe;
    private readonly EmailPasswordRecipe _emailPasswordRecipe;
    private readonly UserRolesRecipe _userRolesRecipe;
    private readonly UserMetadataRecipe _userMetadataRecipe;

    public AuthController(
        SessionRecipe sessionRecipe,
        EmailPasswordRecipe emailPasswordRecipe,
        UserRolesRecipe userRolesRecipe,
        UserMetadataRecipe userMetadataRecipe)
    {
        _sessionRecipe = sessionRecipe ?? throw new ArgumentNullException(nameof(sessionRecipe));
        _emailPasswordRecipe = emailPasswordRecipe ?? throw new ArgumentNullException(nameof(emailPasswordRecipe));
        _userRolesRecipe = userRolesRecipe ?? throw new ArgumentNullException(nameof(userRolesRecipe));
        _userMetadataRecipe = userMetadataRecipe ?? throw new ArgumentNullException(nameof(userMetadataRecipe));
    }

    [HttpPost("signin")]
    public async Task<IActionResult> SignIn([FromBody] SignInRequest request, CancellationToken cancellationToken)
    {
        var user = await _emailPasswordRecipe.SignInAsync(request.Email, request.Password, cancellationToken);
        if (user == null || string.IsNullOrWhiteSpace(user.Id))
        {
            return Unauthorized(new { message = "Invalid credentials." });
        }

        var session = await CreateSessionAndSetCookiesAsync(user.Id, cancellationToken);
        return Ok(new SignInResponse
        {
            UserId = user.Id,
            Email = user.Email,
            AccessToken = session.AccessToken,
            RefreshToken = session.RefreshToken,
            SessionHandle = session.SessionHandle
        });
    }

    [HttpPost("signup")]
    public async Task<IActionResult> SignUp([FromBody] SignUpRequest request, CancellationToken cancellationToken)
    {
        var user = await _emailPasswordRecipe.SignUpAsync(request.Email, request.Password, cancellationToken);
        if (user == null || string.IsNullOrWhiteSpace(user.Id))
        {
            return BadRequest(new { message = "Could not create user." });
        }

        var session = await CreateSessionAndSetCookiesAsync(user.Id, cancellationToken);
        return Ok(new SignInResponse
        {
            UserId = user.Id,
            Email = user.Email,
            AccessToken = session.AccessToken,
            RefreshToken = session.RefreshToken,
            SessionHandle = session.SessionHandle
        });
    }

    [HttpPost("signout")]
    [Authorize]
    public async Task<IActionResult> SignOut(CancellationToken cancellationToken)
    {
        var sessionHandle = HttpContext.Request.Cookies["sSessionHandle"];
        if (!string.IsNullOrWhiteSpace(sessionHandle))
        {
            await _sessionRecipe.RevokeSessionAsync(sessionHandle, cancellationToken);
        }

        ClearCookies();
        return Ok(new { status = "OK" });
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh(CancellationToken cancellationToken)
    {
        if (!HttpContext.Request.Cookies.TryGetValue("sRefreshToken", out var refreshToken) || string.IsNullOrWhiteSpace(refreshToken))
        {
            return Unauthorized(new { message = "No refresh token provided." });
        }

        var antiCsrf = HttpContext.Request.Cookies["sAntiCsrf"];
        var session = await _sessionRecipe.RefreshSessionAsync(refreshToken, antiCsrf, cancellationToken);

        SetCookies(session);
        return Ok(new SessionInfoResponse
        {
            UserId = session.UserId,
            SessionHandle = session.SessionHandle,
            AccessToken = session.AccessToken,
            RefreshToken = session.RefreshToken
        });
    }

    [HttpGet("session")]
    [Authorize]
    public async Task<IActionResult> GetSession(CancellationToken cancellationToken)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        var roles = await _userRolesRecipe.GetRolesAsync(userId, cancellationToken);
        var metadata = await _userMetadataRecipe.GetMetadataAsync(userId, cancellationToken);

        return Ok(new SessionInfoResponse
        {
            UserId = userId,
            Roles = roles.ToList(),
            Metadata = metadata
        });
    }

    private async Task<SessionContainer> CreateSessionAndSetCookiesAsync(string userId, CancellationToken cancellationToken)
    {
        var session = await _sessionRecipe.CreateSessionAsync(userId, cancellationToken: cancellationToken);
        SetCookies(session);
        return session;
    }

    private void SetCookies(SessionContainer session)
    {
        if (!string.IsNullOrWhiteSpace(session.AccessToken))
        {
            HttpContext.Response.Cookies.Append("sAccessToken", session.AccessToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Lax,
                Expires = session.AccessTokenExpiry == DateTime.MinValue ? DateTime.UtcNow.AddHours(1) : session.AccessTokenExpiry
            });
        }

        if (!string.IsNullOrWhiteSpace(session.RefreshToken))
        {
            HttpContext.Response.Cookies.Append("sRefreshToken", session.RefreshToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Lax,
                Expires = session.RefreshTokenExpiry == DateTime.MinValue ? DateTime.UtcNow.AddDays(7) : session.RefreshTokenExpiry
            });
        }

        if (!string.IsNullOrWhiteSpace(session.SessionHandle))
        {
            HttpContext.Response.Cookies.Append("sSessionHandle", session.SessionHandle, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Lax
            });
        }
    }

    private void ClearCookies()
    {
        HttpContext.Response.Cookies.Delete("sAccessToken");
        HttpContext.Response.Cookies.Delete("sRefreshToken");
        HttpContext.Response.Cookies.Delete("sSessionHandle");
        HttpContext.Response.Cookies.Delete("sAntiCsrf");
    }

    public class SignInRequest
    {
        public string Email { get; set; } = "";
        public string Password { get; set; } = "";
    }

    public class SignUpRequest
    {
        public string Email { get; set; } = "";
        public string Password { get; set; } = "";
    }

    public class SignInResponse
    {
        public string UserId { get; set; } = "";
        public string? Email { get; set; }
        public string? AccessToken { get; set; }
        public string? RefreshToken { get; set; }
        public string? SessionHandle { get; set; }
    }

    public class SessionInfoResponse
    {
        public string? UserId { get; set; }
        public List<string> Roles { get; set; } = [];
        public Dictionary<string, object>? Metadata { get; set; }
        public string? AccessToken { get; set; }
        public string? RefreshToken { get; set; }
        public string? SessionHandle { get; set; }
    }
}
