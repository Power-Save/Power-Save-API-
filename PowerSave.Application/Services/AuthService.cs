using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PowerSave.Application.Common.Interfaces;
using PowerSave.Application.Common.Localization;
using PowerSave.Application.Common.Models;
using PowerSave.Application.DTOs;
using PowerSave.Application.Interfaces;
using PowerSave.Application.Mapping;
using PowerSave.Application.Validation;
using PowerSave.Domain.Entities;
using PowerSave.Domain.Enums;

namespace PowerSave.Application.Services;

public class AuthService : IAuthService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IPowerSaveDbContext _dbContext;
    private readonly IPhoneNumberValidator _phoneNumberValidator;
    private readonly IUsernameValidator _usernameValidator;
    private readonly IPasswordPolicyValidator _passwordValidator;
    private readonly IConsentPolicyValidator _consentValidator;
    private readonly IOtpService _otpService;
    private readonly ISmsSender _smsSender;
    private readonly ITokenService _tokenService;
    private readonly IDateTimeProvider _dateTimeProvider;

    public AuthService(
        UserManager<ApplicationUser> userManager,
        IPowerSaveDbContext dbContext,
        IPhoneNumberValidator phoneNumberValidator,
        IUsernameValidator usernameValidator,
        IPasswordPolicyValidator passwordValidator,
        IConsentPolicyValidator consentValidator,
        IOtpService otpService,
        ISmsSender smsSender,
        ITokenService tokenService,
        IDateTimeProvider dateTimeProvider)
    {
        _userManager = userManager;
        _dbContext = dbContext;
        _phoneNumberValidator = phoneNumberValidator;
        _usernameValidator = usernameValidator;
        _passwordValidator = passwordValidator;
        _consentValidator = consentValidator;
        _otpService = otpService;
        _smsSender = smsSender;
        _tokenService = tokenService;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<RegisterResponseDto>> RegisterAsync(
        RegisterRequestDto request,
        string? clientIp,
        CancellationToken cancellationToken = default)
    {

        if (!_consentValidator.Validate(
            request.ConsentToAccountCreationAndDataCollection,
            request.ConsentToProductImprovementDataUsage,
            request.ConsentDocumentVersion,
            out var consentError))
        {
            var code = !request.ConsentToAccountCreationAndDataCollection || !request.ConsentToProductImprovementDataUsage
                ? AuthErrorCode.MissingRequiredConsent
                : AuthErrorCode.UnsupportedConsentVersion;
            return Result<RegisterResponseDto>.Failure(code, 400, consentError);
        }

        if (!_phoneNumberValidator.Validate(request.PhoneNumber, out var normalizedPhone, out var phoneError))
        {
            return Result<RegisterResponseDto>.Failure(AuthErrorCode.InvalidPhoneNumber, 400, phoneError);
        }

        if (!_usernameValidator.Validate(request.Username, out var usernameError))
        {
            var code = string.IsNullOrWhiteSpace(request.Username)
                ? AuthErrorCode.RequiredFieldMissing
                : request.Username.Length < 3 || request.Username.Length > 20
                    ? AuthErrorCode.InvalidUsernameLength
                    : AuthErrorCode.UnsupportedUsernameCharacters;
            return Result<RegisterResponseDto>.Failure(code, 400, usernameError);
        }

        if (!_passwordValidator.Validate(request.Password, out var passwordError))
        {
            var code = string.IsNullOrWhiteSpace(request.Password)
                ? AuthErrorCode.RequiredFieldMissing
                : request.Password.Contains(' ')
                    ? AuthErrorCode.PasswordContainsSpaces
                    : request.Password.Length < 8
                        ? AuthErrorCode.PasswordTooShort
                        : (!request.Password.Any(char.IsLetter) || !request.Password.Any(char.IsDigit))
                            ? AuthErrorCode.PasswordMissingLetterOrDigit
                            : AuthErrorCode.WeakPassword;
            return Result<RegisterResponseDto>.Failure(code, 400, passwordError);
        }

        if (!string.IsNullOrWhiteSpace(request.ConfirmPassword) && request.Password != request.ConfirmPassword)
        {
            return Result<RegisterResponseDto>.Failure(
                AuthErrorCode.PasswordMismatch,
                400,
                AuthMessages.GetArabicMessage(AuthErrorCode.PasswordMismatch));
        }

        var existingUserByName = await _userManager.FindByNameAsync(request.Username);
        if (existingUserByName is not null)
        {
            return Result<RegisterResponseDto>.Failure(AuthErrorCode.DuplicateUsername, 409);
        }

        var existingUserByPhone = await _dbContext.Users
            .FirstOrDefaultAsync(u => u.PhoneNumber == normalizedPhone, cancellationToken);
        if (existingUserByPhone is not null)
        {
            return Result<RegisterResponseDto>.Failure(AuthErrorCode.DuplicatePhoneNumber, 409);
        }

        var now = _dateTimeProvider.UtcNow;

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = request.Username,
            PhoneNumber = normalizedPhone,
            PhoneNumberConfirmed = false,
            IsActive = false,
            CreatedAtUtc = now,
            SecurityStamp = Guid.NewGuid().ToString()
        };

        var identityResult = await _userManager.CreateAsync(user, request.Password);
        if (!identityResult.Succeeded)
        {
            var firstError = identityResult.Errors.FirstOrDefault()?.Description ?? "Failed to create user.";
            return Result<RegisterResponseDto>.Failure(AuthErrorCode.GeneralError, 400, firstError);
        }

        var consent1 = new UserConsent
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            ConsentType = ConsentType.AccountCreationAndDataCollection,
            IsAccepted = true,
            DocumentVersion = request.ConsentDocumentVersion.Trim(),
            PolicyIdentifier = _consentValidator.CurrentDocumentVersion,
            AcceptedAtUtc = now,
            ClientIpAddress = clientIp
        };

        var consent2 = new UserConsent
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            ConsentType = ConsentType.ProductImprovementDataUsage,
            IsAccepted = true,
            DocumentVersion = request.ConsentDocumentVersion.Trim(),
            PolicyIdentifier = _consentValidator.CurrentProductImprovementPolicyVersion,
            AcceptedAtUtc = now,
            ClientIpAddress = clientIp
        };

        _dbContext.UserConsents.AddRange(consent1, consent2);

        var rawOtp = _otpService.GenerateSecureOtp();
        var (hashedOtp, salt) = _otpService.HashOtp(rawOtp);

        var otpRecord = new PhoneVerificationOtp
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            PhoneNumber = normalizedPhone,
            HashedOtp = hashedOtp,
            Salt = salt,
            CreatedAtUtc = now,
            ExpiresAtUtc = now.AddMinutes(5),
            IsUsed = false,
            IsSuperseded = false
        };

        var rateLimitRecord = new UserOtpRateLimit
        {
            UserId = user.Id,
            LastOtpSentAtUtc = now,
            ResendAttemptsInCurrentBatch = 0,
            BatchIndex = 1,
            CooldownUntilUtc = null
        };

        _dbContext.PhoneVerificationOtps.Add(otpRecord);
        _dbContext.UserOtpRateLimits.Add(rateLimitRecord);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var message = $"رمز التحقق الخاص بك في منصة باور سيف هو: {rawOtp}. صالح لمدة 5 دقائق.";
        var smsSuccess = await _smsSender.SendSmsAsync(normalizedPhone, message, cancellationToken);

        var successMessage = smsSuccess
            ? AuthMessages.GetArabicMessage(AuthErrorCode.None) == "حدث خطأ أثناء معالجة الطلب."
                ? "تم إنشاء الحساب بنجاح. يرجى تأكيد رقم الهاتف عبر رمز التحقق المرسل."
                : "تم إنشاء الحساب بنجاح. يرجى تأكيد رقم الهاتف عبر رمز التحقق المرسل."
            : AuthMessages.GetArabicMessage(AuthErrorCode.SmsDeliveryFailed);

        var responseData = user.ToRegisterResponseDto(
            successMessage,
            nextResendAllowedUtc: now.AddSeconds(60),
            remainingWaitSeconds: 60);

        return Result<RegisterResponseDto>.Success(responseData, 201, successMessage);
    }

    public async Task<Result<VerifyPhoneResponseDto>> VerifyPhoneAsync(
        VerifyPhoneRequestDto request,
        CancellationToken cancellationToken = default)
    {

        if (string.IsNullOrWhiteSpace(request.OtpCode) || request.OtpCode.Length != 6 || !request.OtpCode.All(char.IsDigit))
        {
            return Result<VerifyPhoneResponseDto>.Failure(AuthErrorCode.IncorrectOtp, 400);
        }

        var user = await FindUserByIdentifierAsync(request.Identifier, cancellationToken);
        if (user is null)
        {
            return Result<VerifyPhoneResponseDto>.Failure(AuthErrorCode.UserNotFound, 404);
        }

        if (user.PhoneNumberConfirmed && user.IsActive)
        {
            var alreadyVerifiedToken = _tokenService.GenerateToken(user);
            return Result<VerifyPhoneResponseDto>.Success(new VerifyPhoneResponseDto
            {
                IsVerified = true,
                IsActive = true,
                Message = "تم تأكيد رقم الهاتف مسبقاً والحساب مفعل.",
                AccessToken = alreadyVerifiedToken
            });
        }

        var now = _dateTimeProvider.UtcNow;

        var latestOtp = await _dbContext.PhoneVerificationOtps
            .Where(o => o.UserId == user.Id)
            .OrderByDescending(o => o.CreatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        if (latestOtp is null)
        {
            return Result<VerifyPhoneResponseDto>.Failure(AuthErrorCode.IncorrectOtp, 400);
        }

        if (latestOtp.IsUsed || latestOtp.IsSuperseded)
        {
            return Result<VerifyPhoneResponseDto>.Failure(AuthErrorCode.OtpAlreadyUsedOrSuperseded, 400);
        }

        if (now > latestOtp.ExpiresAtUtc)
        {
            return Result<VerifyPhoneResponseDto>.Failure(AuthErrorCode.ExpiredOtp, 400);
        }

        if (!_otpService.VerifyOtp(request.OtpCode, latestOtp.HashedOtp, latestOtp.Salt))
        {
            return Result<VerifyPhoneResponseDto>.Failure(AuthErrorCode.IncorrectOtp, 400);
        }

        latestOtp.IsUsed = true;
        latestOtp.UsedAtUtc = now;

        user.PhoneNumberConfirmed = true;
        user.IsActive = true;

        var updateResult = await _userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            return Result<VerifyPhoneResponseDto>.Failure(AuthErrorCode.ConcurrencyConflict, 409);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        var token = _tokenService.GenerateToken(user);
        var response = new VerifyPhoneResponseDto
        {
            IsVerified = true,
            IsActive = true,
            Message = "تم تأكيد رقم الهاتف وتفعيل الحساب بنجاح.",
            AccessToken = token
        };

        return Result<VerifyPhoneResponseDto>.Success(response, 200, response.Message);
    }

    public async Task<Result<ResendOtpResponseDto>> ResendOtpAsync(
        ResendOtpRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var user = await FindUserByIdentifierAsync(request.Identifier, cancellationToken);
        if (user is null)
        {
            return Result<ResendOtpResponseDto>.Failure(AuthErrorCode.UserNotFound, 404);
        }

        if (user.PhoneNumberConfirmed)
        {
            return Result<ResendOtpResponseDto>.Failure(AuthErrorCode.None, 400, "تم تأكيد رقم الهاتف مسبقاً لهذا الحساب.");
        }

        var now = _dateTimeProvider.UtcNow;

        var rateLimit = await _dbContext.UserOtpRateLimits
            .FirstOrDefaultAsync(r => r.UserId == user.Id, cancellationToken);

        if (rateLimit is null)
        {
            rateLimit = new UserOtpRateLimit
            {
                UserId = user.Id,
                LastOtpSentAtUtc = DateTime.MinValue,
                ResendAttemptsInCurrentBatch = 0,
                BatchIndex = 1
            };
            _dbContext.UserOtpRateLimits.Add(rateLimit);
        }

        if (rateLimit.CooldownUntilUtc.HasValue)
        {
            if (now < rateLimit.CooldownUntilUtc.Value)
            {
                var remainingCooldownSeconds = (int)Math.Ceiling((rateLimit.CooldownUntilUtc.Value - now).TotalSeconds);
                return Result<ResendOtpResponseDto>.RateLimited(
                    AuthErrorCode.ResendLimitReached,
                    remainingCooldownSeconds,
                    rateLimit.CooldownUntilUtc.Value);
            }
            else
            {

                if (rateLimit.BatchIndex == 1)
                {
                    rateLimit.BatchIndex = 2;
                    rateLimit.ResendAttemptsInCurrentBatch = 0;
                    rateLimit.CooldownUntilUtc = null;
                }
                else
                {

                    rateLimit.BatchIndex = 1;
                    rateLimit.ResendAttemptsInCurrentBatch = 0;
                    rateLimit.CooldownUntilUtc = null;
                }
            }
        }

        var elapsedSeconds = (now - rateLimit.LastOtpSentAtUtc).TotalSeconds;
        if (elapsedSeconds < 60)
        {
            var waitSeconds = 60 - (int)Math.Floor(elapsedSeconds);
            var nextAllowed = rateLimit.LastOtpSentAtUtc.AddSeconds(60);
            return Result<ResendOtpResponseDto>.RateLimited(
                AuthErrorCode.ResendRequestedTooEarly,
                waitSeconds,
                nextAllowed);
        }

        if (rateLimit.ResendAttemptsInCurrentBatch >= 3)
        {
            if (rateLimit.BatchIndex == 1)
            {
                rateLimit.CooldownUntilUtc = now.AddHours(1);
                await _dbContext.SaveChangesAsync(cancellationToken);
                return Result<ResendOtpResponseDto>.RateLimited(
                    AuthErrorCode.ResendLimitReached,
                    3600,
                    rateLimit.CooldownUntilUtc.Value);
            }
            else
            {
                rateLimit.CooldownUntilUtc = now.AddHours(24);
                await _dbContext.SaveChangesAsync(cancellationToken);
                return Result<ResendOtpResponseDto>.RateLimited(
                    AuthErrorCode.ResendLimitReached,
                    86400,
                    rateLimit.CooldownUntilUtc.Value);
            }
        }

        rateLimit.ResendAttemptsInCurrentBatch++;
        rateLimit.LastOtpSentAtUtc = now;

        var activeOtps = await _dbContext.PhoneVerificationOtps
            .Where(o => o.UserId == user.Id && !o.IsUsed && !o.IsSuperseded)
            .ToListAsync(cancellationToken);

        foreach (var otp in activeOtps)
        {
            otp.IsSuperseded = true;
            otp.SupersededAtUtc = now;
        }

        var newOtp = _otpService.GenerateSecureOtp();
        var (hashedOtp, salt) = _otpService.HashOtp(newOtp);

        var newOtpRecord = new PhoneVerificationOtp
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            PhoneNumber = user.PhoneNumber!,
            HashedOtp = hashedOtp,
            Salt = salt,
            CreatedAtUtc = now,
            ExpiresAtUtc = now.AddMinutes(5),
            IsUsed = false,
            IsSuperseded = false
        };

        _dbContext.PhoneVerificationOtps.Add(newOtpRecord);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var message = $"رمز التحقق الجديد الخاص بك في منصة باور سيف هو: {newOtp}. صالح لمدة 5 دقائق.";
        await _smsSender.SendSmsAsync(user.PhoneNumber!, message, cancellationToken);

        var remainingInBatch = 3 - rateLimit.ResendAttemptsInCurrentBatch;
        var nextAllowedResend = now.AddSeconds(60);

        var response = new ResendOtpResponseDto
        {
            Success = true,
            Message = "تم إرسال رمز تحقق جديد بنجاح.",
            RemainingResendsInBatch = remainingInBatch,
            NextAllowedResendUtc = nextAllowedResend,
            RemainingWaitSeconds = 60
        };

        return Result<ResendOtpResponseDto>.Success(response, 200, response.Message);
    }

    public async Task<Result<LoginResponseDto>> LoginAsync(
        LoginRequestDto request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Identifier) || string.IsNullOrWhiteSpace(request.Password))
        {
            return Result<LoginResponseDto>.Failure(AuthErrorCode.RequiredFieldMissing, 400);
        }

        var user = await FindUserByIdentifierAsync(request.Identifier, cancellationToken);
        if (user is null)
        {

            return Result<LoginResponseDto>.Failure(AuthErrorCode.InvalidCredentials, 401);
        }

        var passwordValid = await _userManager.CheckPasswordAsync(user, request.Password);
        if (!passwordValid)
        {

            return Result<LoginResponseDto>.Failure(AuthErrorCode.InvalidCredentials, 401);
        }

        if (!user.PhoneNumberConfirmed)
        {
            return Result<LoginResponseDto>.Failure(AuthErrorCode.AccountPhoneUnverified, 403);
        }

        if (!user.IsActive)
        {
            return Result<LoginResponseDto>.Failure(AuthErrorCode.AccountInactive, 403);
        }

        user.LastLoginAtUtc = _dateTimeProvider.UtcNow;
        await _userManager.UpdateAsync(user);

        var token = _tokenService.GenerateToken(user);
        var response = new LoginResponseDto
        {
            AccessToken = token,
            ExpiresInSeconds = _tokenService.ExpirationSeconds,
            TokenType = "Bearer",
            User = user.ToUserProfileDto()
        };

        return Result<LoginResponseDto>.Success(response, 200, "تم تسجيل الدخول بنجاح.");
    }

    public async Task<Result<UserProfileResponseDto>> GetCurrentUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            return Result<UserProfileResponseDto>.Failure(AuthErrorCode.UserNotFound, 404);
        }

        return Result<UserProfileResponseDto>.Success(user.ToUserProfileDto(), 200);
    }

    private async Task<ApplicationUser?> FindUserByIdentifierAsync(string identifier, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(identifier))
            return null;

        var trimmed = identifier.Trim();

        var userByName = await _userManager.FindByNameAsync(trimmed);
        if (userByName is not null)
            return userByName;

        if (_phoneNumberValidator.Validate(trimmed, out var normalizedPhone, out _))
        {
            var userByPhone = await _dbContext.Users
                .FirstOrDefaultAsync(u => u.PhoneNumber == normalizedPhone, cancellationToken);
            if (userByPhone is not null)
                return userByPhone;
        }

        if (Guid.TryParse(trimmed, out var guidId))
        {
            return await _userManager.FindByIdAsync(guidId.ToString());
        }

        return null;
    }
}

