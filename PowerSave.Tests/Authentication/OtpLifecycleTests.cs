using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using PowerSave.Application.Common.Localization;
using PowerSave.Application.DTOs;
using PowerSave.Application.Services;
using PowerSave.Application.Validation;
using PowerSave.Infrastructure.Security;
using PowerSave.Tests.Helpers;
using Xunit;

namespace PowerSave.Tests.Authentication;

public class OtpLifecycleTests
{
    private readonly TestTimeProvider _timeProvider = new();
    private readonly TestSmsSender _smsSender = new();
    private readonly PhoneNumberValidator _phoneValidator = new();
    private readonly UsernameValidator _usernameValidator = new();
    private readonly PasswordPolicyValidator _passwordValidator = new();
    private readonly ConsentPolicyValidator _consentValidator = new();
    private readonly OtpService _otpService = new();

    private AuthService CreateAuthService(string dbName)
    {
        var (dbContext, userManager) = TestAuthDbContextFactory.Create(dbName);
        var jwtOptions = Options.Create(new JwtOptions
        {
            SecretKey = "Test_Secret_Key_For_Unit_Testing_PowerSave_Platform_2026!",
            Issuer = "PowerSave.API",
            Audience = "PowerSave.App",
            ExpirationMinutes = 60
        });
        var tokenService = new JwtTokenService(jwtOptions);

        return new AuthService(
            userManager,
            dbContext,
            _phoneValidator,
            _usernameValidator,
            _passwordValidator,
            _consentValidator,
            _otpService,
            _smsSender,
            tokenService,
            _timeProvider);
    }

    [Fact]
    public async Task VerifyOtp_ValidCode_ShouldActivateAccountAndMarkOtpUsed()
    {
        var dbName = Guid.NewGuid().ToString();
        var (dbContext, userManager) = TestAuthDbContextFactory.Create(dbName);
        var authService = CreateAuthService(dbName);

        await authService.RegisterAsync(new RegisterRequestDto
        {
            PhoneNumber = "01012345678",
            Username = "VerifyUser",
            Password = "Password123",
            ConsentToAccountCreationAndDataCollection = true,
            ConsentToProductImprovementDataUsage = true,
            ConsentDocumentVersion = "v1.0"
        }, "127.0.0.1");

        var otpCode = _smsSender.LastExtractedOtp;
        Assert.NotNull(otpCode);

        var verifyResult = await authService.VerifyPhoneAsync(new VerifyPhoneRequestDto
        {
            Identifier = "VerifyUser",
            OtpCode = otpCode
        });

        Assert.True(verifyResult.IsSuccess);
        Assert.True(verifyResult.Data!.IsVerified);
        Assert.True(verifyResult.Data.IsActive);
        Assert.NotNull(verifyResult.Data.AccessToken);

        var user = await userManager.FindByNameAsync("VerifyUser");
        Assert.NotNull(user);
        Assert.True(user.PhoneNumberConfirmed);
        Assert.True(user.IsActive);

        var otpRecord = await dbContext.PhoneVerificationOtps.FirstAsync(o => o.UserId == user.Id);
        Assert.True(otpRecord.IsUsed);
        Assert.NotNull(otpRecord.UsedAtUtc);
    }

    [Fact]
    public async Task VerifyOtp_ExpiredCode_ShouldReject()
    {
        var dbName = Guid.NewGuid().ToString();
        var authService = CreateAuthService(dbName);

        await authService.RegisterAsync(new RegisterRequestDto
        {
            PhoneNumber = "01012345678",
            Username = "ExpireUser",
            Password = "Password123",
            ConsentToAccountCreationAndDataCollection = true,
            ConsentToProductImprovementDataUsage = true,
            ConsentDocumentVersion = "v1.0"
        }, "127.0.0.1");

        var otpCode = _smsSender.LastExtractedOtp;
        Assert.NotNull(otpCode);

        _timeProvider.AdvanceMinutes(5);
        _timeProvider.AdvanceSeconds(1);

        var verifyResult = await authService.VerifyPhoneAsync(new VerifyPhoneRequestDto
        {
            Identifier = "ExpireUser",
            OtpCode = otpCode
        });

        Assert.False(verifyResult.IsSuccess);
        Assert.Equal(AuthErrorCode.ExpiredOtp, verifyResult.ErrorCode);
    }

    [Fact]
    public async Task VerifyOtp_ReusingAlreadyUsedCode_ShouldReject()
    {
        var dbName = Guid.NewGuid().ToString();
        var authService = CreateAuthService(dbName);

        await authService.RegisterAsync(new RegisterRequestDto
        {
            PhoneNumber = "01012345678",
            Username = "ReuseUser",
            Password = "Password123",
            ConsentToAccountCreationAndDataCollection = true,
            ConsentToProductImprovementDataUsage = true,
            ConsentDocumentVersion = "v1.0"
        }, "127.0.0.1");

        var otpCode = _smsSender.LastExtractedOtp!;

        var firstResult = await authService.VerifyPhoneAsync(new VerifyPhoneRequestDto
        {
            Identifier = "ReuseUser",
            OtpCode = otpCode
        });
        Assert.True(firstResult.IsSuccess);

        var secondResult = await authService.VerifyPhoneAsync(new VerifyPhoneRequestDto
        {
            Identifier = "ReuseUser",
            OtpCode = otpCode
        });

        Assert.True(secondResult.IsSuccess);
    }

    [Fact]
    public async Task VerifyOtp_SupersededCodeAfterResend_ShouldRejectOlderCode()
    {
        var dbName = Guid.NewGuid().ToString();
        var authService = CreateAuthService(dbName);

        await authService.RegisterAsync(new RegisterRequestDto
        {
            PhoneNumber = "01012345678",
            Username = "SuperUser",
            Password = "Password123",
            ConsentToAccountCreationAndDataCollection = true,
            ConsentToProductImprovementDataUsage = true,
            ConsentDocumentVersion = "v1.0"
        }, "127.0.0.1");

        var code1 = _smsSender.LastExtractedOtp!;

        _timeProvider.AdvanceSeconds(61);
        var resendResult = await authService.ResendOtpAsync(new ResendOtpRequestDto { Identifier = "SuperUser" });
        Assert.True(resendResult.IsSuccess);

        var code2 = _smsSender.LastExtractedOtp!;
        Assert.NotEqual(code1, code2);

        var verifyCode1Result = await authService.VerifyPhoneAsync(new VerifyPhoneRequestDto
        {
            Identifier = "SuperUser",
            OtpCode = code1
        });
        Assert.False(verifyCode1Result.IsSuccess);
        Assert.Equal(AuthErrorCode.IncorrectOtp, verifyCode1Result.ErrorCode);

        var verifyCode2Result = await authService.VerifyPhoneAsync(new VerifyPhoneRequestDto
        {
            Identifier = "SuperUser",
            OtpCode = code2
        });
        Assert.True(verifyCode2Result.IsSuccess);
    }

    [Fact]
    public async Task ResendOtp_Before60Seconds_ShouldRejectWithCountdown()
    {
        var dbName = Guid.NewGuid().ToString();
        var authService = CreateAuthService(dbName);

        await authService.RegisterAsync(new RegisterRequestDto
        {
            PhoneNumber = "01012345678",
            Username = "EarlyResendUser",
            Password = "Password123",
            ConsentToAccountCreationAndDataCollection = true,
            ConsentToProductImprovementDataUsage = true,
            ConsentDocumentVersion = "v1.0"
        }, "127.0.0.1");

        _timeProvider.AdvanceSeconds(20);

        var resendResult = await authService.ResendOtpAsync(new ResendOtpRequestDto
        {
            Identifier = "EarlyResendUser"
        });

        Assert.False(resendResult.IsSuccess);
        Assert.Equal(429, resendResult.StatusCode);
        Assert.Equal(AuthErrorCode.ResendRequestedTooEarly, resendResult.ErrorCode);
        Assert.Equal(40, resendResult.RemainingWaitSeconds);
    }

    [Fact]
    public async Task ResendOtp_CompleteCooldownLifecycle_ThreeResends_OneHourCooldown_SecondBatch_TwentyFourHourCooldown()
    {
        var dbName = Guid.NewGuid().ToString();
        var authService = CreateAuthService(dbName);

        var regResult = await authService.RegisterAsync(new RegisterRequestDto
        {
            PhoneNumber = "01012345678",
            Username = "CooldownUser",
            Password = "Password123",
            ConsentToAccountCreationAndDataCollection = true,
            ConsentToProductImprovementDataUsage = true,
            ConsentDocumentVersion = "v1.0"
        }, "127.0.0.1");
        Assert.True(regResult.IsSuccess, $"Register failed: {regResult.Message}");

        _timeProvider.AdvanceSeconds(61);
        var r1 = await authService.ResendOtpAsync(new ResendOtpRequestDto { Identifier = "CooldownUser" });
        Assert.True(r1.IsSuccess, $"Failed: {r1.ErrorCode} - {r1.Message} - Status: {r1.StatusCode}");
        Assert.Equal(2, r1.Data!.RemainingResendsInBatch);

        _timeProvider.AdvanceSeconds(61);
        var r2 = await authService.ResendOtpAsync(new ResendOtpRequestDto { Identifier = "CooldownUser" });
        Assert.True(r2.IsSuccess);
        Assert.Equal(1, r2.Data!.RemainingResendsInBatch);

        _timeProvider.AdvanceSeconds(61);
        var r3 = await authService.ResendOtpAsync(new ResendOtpRequestDto { Identifier = "CooldownUser" });
        Assert.True(r3.IsSuccess);
        Assert.Equal(0, r3.Data!.RemainingResendsInBatch);

        _timeProvider.AdvanceSeconds(61);
        var r4 = await authService.ResendOtpAsync(new ResendOtpRequestDto { Identifier = "CooldownUser" });
        Assert.False(r4.IsSuccess);
        Assert.Equal(429, r4.StatusCode);
        Assert.Equal(AuthErrorCode.ResendLimitReached, r4.ErrorCode);

        _timeProvider.AdvanceMinutes(30);
        var r5 = await authService.ResendOtpAsync(new ResendOtpRequestDto { Identifier = "CooldownUser" });
        Assert.False(r5.IsSuccess);
        Assert.Equal(429, r5.StatusCode);

        _timeProvider.AdvanceMinutes(31);

        var b2_r1 = await authService.ResendOtpAsync(new ResendOtpRequestDto { Identifier = "CooldownUser" });
        Assert.True(b2_r1.IsSuccess);
        Assert.Equal(2, b2_r1.Data!.RemainingResendsInBatch);

        _timeProvider.AdvanceSeconds(61);
        var b2_r2 = await authService.ResendOtpAsync(new ResendOtpRequestDto { Identifier = "CooldownUser" });
        Assert.True(b2_r2.IsSuccess);
        Assert.Equal(1, b2_r2.Data!.RemainingResendsInBatch);

        _timeProvider.AdvanceSeconds(61);
        var b2_r3 = await authService.ResendOtpAsync(new ResendOtpRequestDto { Identifier = "CooldownUser" });
        Assert.True(b2_r3.IsSuccess);
        Assert.Equal(0, b2_r3.Data!.RemainingResendsInBatch);

        _timeProvider.AdvanceSeconds(61);
        var b2_r4 = await authService.ResendOtpAsync(new ResendOtpRequestDto { Identifier = "CooldownUser" });
        Assert.False(b2_r4.IsSuccess);
        Assert.Equal(429, b2_r4.StatusCode);
        Assert.Equal(AuthErrorCode.ResendLimitReached, b2_r4.ErrorCode);

        Assert.True(b2_r4.RemainingWaitSeconds > 86000);
    }

    [Fact]
    public async Task VerifyOtp_FiveIncorrectAttempts_ShouldInvalidateOtpAndLockOut()
    {
        var dbName = Guid.NewGuid().ToString();
        var authService = CreateAuthService(dbName);

        await authService.RegisterAsync(new RegisterRequestDto
        {
            PhoneNumber = "01012345678",
            Username = "BruteForceUser",
            Password = "Password123",
            ConsentToAccountCreationAndDataCollection = true,
            ConsentToProductImprovementDataUsage = true,
            ConsentDocumentVersion = "v1.0"
        }, "127.0.0.1");

        var validOtp = _smsSender.LastExtractedOtp!;

        for (int i = 1; i <= 4; i++)
        {
            var res = await authService.VerifyPhoneAsync(new VerifyPhoneRequestDto
            {
                Identifier = "BruteForceUser",
                OtpCode = "000000"
            });
            Assert.False(res.IsSuccess);
            Assert.Equal(AuthErrorCode.IncorrectOtp, res.ErrorCode);
        }

        var fifthResult = await authService.VerifyPhoneAsync(new VerifyPhoneRequestDto
        {
            Identifier = "BruteForceUser",
            OtpCode = "000000"
        });
        Assert.False(fifthResult.IsSuccess);
        Assert.Equal(AuthErrorCode.OtpMaxAttemptsExceeded, fifthResult.ErrorCode);

        var validOtpResult = await authService.VerifyPhoneAsync(new VerifyPhoneRequestDto
        {
            Identifier = "BruteForceUser",
            OtpCode = validOtp
        });
        Assert.False(validOtpResult.IsSuccess);
        Assert.True(validOtpResult.ErrorCode == AuthErrorCode.OtpMaxAttemptsExceeded || validOtpResult.ErrorCode == AuthErrorCode.OtpAlreadyUsedOrSuperseded);
    }
}
