using PowerSave.Application.Common.Models;
using PowerSave.Application.DTOs;

namespace PowerSave.Application.Interfaces;

public interface IAuthService
{
    Task<Result<RegisterResponseDto>> RegisterAsync(RegisterRequestDto request, string? clientIp, CancellationToken cancellationToken = default);
    Task<Result<VerifyPhoneResponseDto>> VerifyPhoneAsync(VerifyPhoneRequestDto request, CancellationToken cancellationToken = default);
    Task<Result<ResendOtpResponseDto>> ResendOtpAsync(ResendOtpRequestDto request, CancellationToken cancellationToken = default);
    Task<Result<LoginResponseDto>> LoginAsync(LoginRequestDto request, CancellationToken cancellationToken = default);
    Task<Result<UserProfileResponseDto>> GetCurrentUserAsync(Guid userId, CancellationToken cancellationToken = default);
}

