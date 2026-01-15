using Identity.Application;
using Identity.Application.DTOs;
using Microsoft.AspNetCore.Mvc;
using SharedKernel;

namespace Identity.Api.Controllers;

[ApiController]
[Route("api/identity")]
public sealed class IdentityController(
    IAuth0Service auth0Service,
    HeaderUserContext userContext) : ControllerBase
{
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await auth0Service.RegisterAsync(request, cancellationToken);
            return Ok(result);
        }
        catch (HttpRequestException ex)
        {
            return BadRequest(new { error = "Registration failed", details = ex.Message });
        }
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await auth0Service.LoginAsync(request, cancellationToken);
            return Ok(result);
        }
        catch (HttpRequestException ex)
        {
            return Unauthorized(new { error = "Invalid credentials", details = ex.Message });
        }
    }

    [HttpGet("login/google")]
    public IActionResult LoginWithGoogle()
    {
        var redirectUri = $"{Request.Scheme}://{Request.Host}/api/identity/callback";
        var loginUrl = auth0Service.GetGoogleLoginUrl(redirectUri);
        return Redirect(loginUrl);
    }

    [HttpGet("callback")]
    public async Task<IActionResult> Callback([FromQuery] string code, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(code))
        {
            return BadRequest(new { error = "Authorization code is required" });
        }

        try
        {
            var redirectUri = $"{Request.Scheme}://{Request.Host}/api/identity/callback";
            var result = await auth0Service.ExchangeCodeForTokenAsync(code, redirectUri, cancellationToken);
            
            // Return tokens as JSON (in production, you might redirect to frontend with tokens in URL or set cookies)
            return Ok(result);
        }
        catch (HttpRequestException ex)
        {
            return BadRequest(new { error = "Token exchange failed", details = ex.Message });
        }
    }

    [HttpGet("me")]
    public IActionResult Me()
    {
        // Gateway already validated authentication, user context is in headers
        if (!userContext.IsAuthenticated)
        {
            return Unauthorized(new { error = "User context not found. Request must go through API Gateway." });
        }

        return Ok(new
        {
            userId = userContext.UserId,
            email = userContext.Email
        });
    }
}

