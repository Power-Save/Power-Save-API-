using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PowerSave.Application.DTOs;
using PowerSave.Application.Interfaces;

namespace Power_Save.Controllers;

[ApiController]
[Route("api/auth")]
[Produces("application/json")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("register")]
    [ProducesResponseType(typeof(ApiResponse<RegisterResponseDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register([FromBody] RegisterRequestDto request)
    {
        var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString();
        var result = await _authService.RegisterAsync(request, clientIp);

        if (!result.IsSuccess)
        {
            return StatusCode(result.StatusCode, ApiResponse.Fail(result.Message, result.ErrorCode.ToString()));
        }

        return StatusCode(StatusCodes.Status201Created, ApiResponse<RegisterResponseDto>.Ok(result.Data!, result.Message));
    }

    [HttpPost("verify-phone")]
    [ProducesResponseType(typeof(ApiResponse<VerifyPhoneResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> VerifyPhone([FromBody] VerifyPhoneRequestDto request)
    {
        var result = await _authService.VerifyPhoneAsync(request);

        if (!result.IsSuccess)
        {
            return StatusCode(result.StatusCode, ApiResponse.Fail(result.Message, result.ErrorCode.ToString()));
        }

        return Ok(ApiResponse<VerifyPhoneResponseDto>.Ok(result.Data!, result.Message));
    }

    [HttpPost("resend-otp")]
    [ProducesResponseType(typeof(ApiResponse<ResendOtpResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> ResendOtp([FromBody] ResendOtpRequestDto request)
    {
        var result = await _authService.ResendOtpAsync(request);

        if (!result.IsSuccess)
        {
            if (result.StatusCode == StatusCodes.Status429TooManyRequests && result.RemainingWaitSeconds.HasValue)
            {
                Response.Headers.Append("Retry-After", result.RemainingWaitSeconds.Value.ToString());
            }

            return StatusCode(
                result.StatusCode,
                ApiResponse.Fail(result.Message, result.ErrorCode.ToString(), result.RemainingWaitSeconds, result.NextAllowedResendUtc));
        }

        return Ok(ApiResponse<ResendOtpResponseDto>.Ok(result.Data!, result.Message));
    }

    [HttpPost("login")]
    [ProducesResponseType(typeof(ApiResponse<LoginResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
    {
        var result = await _authService.LoginAsync(request);

        if (!result.IsSuccess)
        {
            return StatusCode(result.StatusCode, ApiResponse.Fail(result.Message, result.ErrorCode.ToString()));
        }

        return Ok(ApiResponse<LoginResponseDto>.Ok(result.Data!, result.Message));
    }

    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<UserProfileResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Me()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(ApiResponse.Fail("غير مصرح بالوصول.", "UNAUTHORIZED"));
        }

        var result = await _authService.GetCurrentUserAsync(userId);
        if (!result.IsSuccess)
        {
            return StatusCode(result.StatusCode, ApiResponse.Fail(result.Message, result.ErrorCode.ToString()));
        }

        return Ok(ApiResponse<UserProfileResponseDto>.Ok(result.Data!));
    }
}

