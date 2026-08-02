using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using OppSignal.Api.Infrastructure;
using OppSignal.Application.Auth;

namespace OppSignal.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly IAuthService _auth;

    public AuthController(IAuthService auth) => _auth = auth;

    [HttpPost("register")]
    [EnableRateLimiting("auth")]
    [AllowAnonymous]
    public async Task<IActionResult> Register(
        [FromBody] RegisterRequest request, [FromServices] IValidator<RegisterRequest> validator, CancellationToken ct)
    {
        await validator.ValidateAndThrowAsync(request, ct);
        await _auth.RegisterAsync(request, ct);
        return Ok(new { message = "Account created. Check your email to confirm your address." });
    }

    [HttpPost("login")]
    [EnableRateLimiting("auth")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthTokens>> Login(
        [FromBody] LoginRequest request, [FromServices] IValidator<LoginRequest> validator, CancellationToken ct)
    {
        await validator.ValidateAndThrowAsync(request, ct);
        return Ok(await _auth.LoginAsync(request, ct));
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthTokens>> Refresh([FromBody] RefreshRequest request, CancellationToken ct)
        => Ok(await _auth.RefreshAsync(request.RefreshToken, ct));

    [HttpPost("logout")]
    [AllowAnonymous]
    public async Task<IActionResult> Logout([FromBody] RefreshRequest request, CancellationToken ct)
    {
        await _auth.LogoutAsync(request.RefreshToken, ct);
        return NoContent();
    }

    [HttpPost("confirm-email")]
    [AllowAnonymous]
    public async Task<IActionResult> ConfirmEmail([FromBody] ConfirmEmailRequest request, CancellationToken ct)
    {
        await _auth.ConfirmEmailAsync(request, ct);
        return Ok(new { message = "Email confirmed. You can now sign in." });
    }

    [HttpPost("resend-verification")]
    [EnableRateLimiting("auth")]
    [AllowAnonymous]
    public async Task<IActionResult> ResendVerification([FromBody] ResendVerificationRequest request, CancellationToken ct)
    {
        await _auth.ResendVerificationAsync(request.Email, ct);
        return Ok(new { message = "If that account exists and is unverified, a new link has been sent." });
    }

    [HttpPost("forgot-password")]
    [EnableRateLimiting("auth")]
    [AllowAnonymous]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request, CancellationToken ct)
    {
        await _auth.ForgotPasswordAsync(request.Email, ct);
        return Ok(new { message = "If that account exists, a reset link has been sent." });
    }

    [HttpPost("reset-password")]
    [EnableRateLimiting("auth")]
    [AllowAnonymous]
    public async Task<IActionResult> ResetPassword(
        [FromBody] ResetPasswordRequest request, [FromServices] IValidator<ResetPasswordRequest> validator, CancellationToken ct)
    {
        await validator.ValidateAndThrowAsync(request, ct);
        await _auth.ResetPasswordAsync(request, ct);
        return Ok(new { message = "Your password has been reset. You can now sign in." });
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<MeDto>> Me(CancellationToken ct)
        => Ok(await _auth.GetMeAsync(User.GetUserId(), ct));

    [HttpPut("me")]
    [Authorize]
    public async Task<IActionResult> UpdateAccount([FromBody] UpdateAccountRequest request, CancellationToken ct)
    {
        await _auth.UpdateAccountAsync(User.GetUserId(), request, ct);
        return NoContent();
    }

    [HttpPost("change-password")]
    [Authorize]
    public async Task<ActionResult<AuthTokens>> ChangePassword(
        [FromBody] ChangePasswordRequest request, [FromServices] IValidator<ChangePasswordRequest> validator, CancellationToken ct)
    {
        await validator.ValidateAndThrowAsync(request, ct);
        return Ok(await _auth.ChangePasswordAsync(User.GetUserId(), request, ct));
    }
}
