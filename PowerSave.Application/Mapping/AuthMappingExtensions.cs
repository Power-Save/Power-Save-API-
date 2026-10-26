using PowerSave.Application.DTOs;
using PowerSave.Domain.Entities;

namespace PowerSave.Application.Mapping;

public static class AuthMappingExtensions
{
    public static UserProfileResponseDto ToUserProfileDto(this ApplicationUser user)
    {
        return new UserProfileResponseDto
        {
            Id = user.Id,
            Username = user.UserName ?? string.Empty,
            PhoneNumber = user.PhoneNumber ?? string.Empty,
            IsPhoneVerified = user.PhoneNumberConfirmed,
            IsActive = user.IsActive,
            CreatedAtUtc = user.CreatedAtUtc
        };
    }

    public static RegisterResponseDto ToRegisterResponseDto(
        this ApplicationUser user,
        string message,
        DateTime? nextResendAllowedUtc = null,
        int? remainingWaitSeconds = null)
    {
        return new RegisterResponseDto
        {
            UserId = user.Id,
            Username = user.UserName ?? string.Empty,
            PhoneNumber = user.PhoneNumber ?? string.Empty,
            IsPhoneVerified = user.PhoneNumberConfirmed,
            Message = message,
            NextResendAllowedUtc = nextResendAllowedUtc,
            RemainingWaitSeconds = remainingWaitSeconds
        };
    }
}

