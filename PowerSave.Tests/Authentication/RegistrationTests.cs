using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using PowerSave.Application.Common.Localization;
using PowerSave.Application.DTOs;
using PowerSave.Application.Services;
using PowerSave.Application.Validation;
using PowerSave.Domain.Enums;
using PowerSave.Infrastructure.Security;
using PowerSave.Tests.Helpers;
using Xunit;

namespace PowerSave.Tests.Authentication;

public class RegistrationTests
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
    public async Task Register_ValidRequest_ShouldCreateUnverifiedAccountAndDispatchOtp()
    {
        var dbName = Guid.NewGuid().ToString();
        var (dbContext, userManager) = TestAuthDbContextFactory.Create(dbName);
        var authService = CreateAuthService(dbName);

        var request = new RegisterRequestDto
        {
            PhoneNumber = "01012345678",
            Username = "Mohamed_2026",
            Password = "MySecurePassword1",
            ConsentToAccountCreationAndDataCollection = true,
            ConsentToProductImprovementDataUsage = true,
            ConsentDocumentVersion = "v1.0"
        };

        var result = await authService.RegisterAsync(request, "192.168.1.100");

        Assert.True(result.IsSuccess);
        Assert.Equal(201, result.StatusCode);
        Assert.NotNull(result.Data);
        Assert.Equal("Mohamed_2026", result.Data.Username);
        Assert.Equal("01012345678", result.Data.PhoneNumber);
        Assert.False(result.Data.IsPhoneVerified);

        var user = await userManager.FindByNameAsync("Mohamed_2026");
        Assert.NotNull(user);
        Assert.False(user.PhoneNumberConfirmed);
        Assert.False(user.IsActive);
        Assert.Equal("01012345678", user.PhoneNumber);

        Assert.NotEqual("MySecurePassword1", user.PasswordHash);
        Assert.True(await userManager.CheckPasswordAsync(user, "MySecurePassword1"));

        var consents = await dbContext.UserConsents.Where(c => c.UserId == user.Id).ToListAsync();
        Assert.Equal(2, consents.Count);
        Assert.Contains(consents, c => c.ConsentType == ConsentType.AccountCreationAndDataCollection && c.IsAccepted);
        Assert.Contains(consents, c => c.ConsentType == ConsentType.ProductImprovementDataUsage && c.IsAccepted);
        Assert.All(consents, c => Assert.Equal(_timeProvider.UtcNow, c.AcceptedAtUtc));

        var otpRecord = await dbContext.PhoneVerificationOtps.FirstOrDefaultAsync(o => o.UserId == user.Id);
        Assert.NotNull(otpRecord);
        Assert.NotEqual(_smsSender.LastExtractedOtp, otpRecord.HashedOtp);
        Assert.False(otpRecord.IsUsed);
        Assert.False(otpRecord.IsSuperseded);
        Assert.Equal(_timeProvider.UtcNow.AddMinutes(5), otpRecord.ExpiresAtUtc);

        Assert.NotEmpty(_smsSender.SentMessages);
        Assert.Contains(_smsSender.SentMessages, m => m.PhoneNumber == "01012345678");
    }

    [Fact]
    public async Task Register_TenDigitPhoneNumber_ShouldNormalizeWithLeadingZero()
    {
        var dbName = Guid.NewGuid().ToString();
        var (dbContext, userManager) = TestAuthDbContextFactory.Create(dbName);
        var authService = CreateAuthService(dbName);

        var request = new RegisterRequestDto
        {
            PhoneNumber = "1123456789",
            Username = "Ahmed_User",
            Password = "Password123",
            ConsentToAccountCreationAndDataCollection = true,
            ConsentToProductImprovementDataUsage = true,
            ConsentDocumentVersion = "v1.0"
        };

        var result = await authService.RegisterAsync(request, "127.0.0.1");

        Assert.True(result.IsSuccess);
        Assert.Equal("01123456789", result.Data!.PhoneNumber);

        var user = await userManager.FindByNameAsync("Ahmed_User");
        Assert.NotNull(user);
        Assert.Equal("01123456789", user.PhoneNumber);
    }

    [Fact]
    public async Task Register_DuplicateUsernameDifferentCase_ShouldConflict()
    {
        var dbName = Guid.NewGuid().ToString();
        var authService = CreateAuthService(dbName);

        var request1 = new RegisterRequestDto
        {
            PhoneNumber = "01011111111",
            Username = "Mohamed_10",
            Password = "ValidPassword1",
            ConsentToAccountCreationAndDataCollection = true,
            ConsentToProductImprovementDataUsage = true,
            ConsentDocumentVersion = "v1.0"
        };

        var request2 = new RegisterRequestDto
        {
            PhoneNumber = "01022222222",
            Username = "mohamed_10",
            Password = "ValidPassword2",
            ConsentToAccountCreationAndDataCollection = true,
            ConsentToProductImprovementDataUsage = true,
            ConsentDocumentVersion = "v1.0"
        };

        var result1 = await authService.RegisterAsync(request1, "127.0.0.1");
        var result2 = await authService.RegisterAsync(request2, "127.0.0.1");

        Assert.True(result1.IsSuccess);
        Assert.False(result2.IsSuccess);
        Assert.Equal(409, result2.StatusCode);
        Assert.Equal(AuthErrorCode.DuplicateUsername, result2.ErrorCode);
    }

    [Fact]
    public async Task Register_DuplicatePhoneNumber_ShouldConflict()
    {
        var dbName = Guid.NewGuid().ToString();
        var authService = CreateAuthService(dbName);

        var request1 = new RegisterRequestDto
        {
            PhoneNumber = "01012345678",
            Username = "User_One",
            Password = "ValidPassword1",
            ConsentToAccountCreationAndDataCollection = true,
            ConsentToProductImprovementDataUsage = true,
            ConsentDocumentVersion = "v1.0"
        };

        var request2 = new RegisterRequestDto
        {
            PhoneNumber = "1012345678",
            Username = "User_Two",
            Password = "ValidPassword2",
            ConsentToAccountCreationAndDataCollection = true,
            ConsentToProductImprovementDataUsage = true,
            ConsentDocumentVersion = "v1.0"
        };

        var result1 = await authService.RegisterAsync(request1, "127.0.0.1");
        var result2 = await authService.RegisterAsync(request2, "127.0.0.1");

        Assert.True(result1.IsSuccess);
        Assert.False(result2.IsSuccess);
        Assert.Equal(409, result2.StatusCode);
        Assert.Equal(AuthErrorCode.DuplicatePhoneNumber, result2.ErrorCode);
    }

    [Fact]
    public async Task Register_WeakPasswordRepeatedCharacters_ShouldReject()
    {
        var dbName = Guid.NewGuid().ToString();
        var authService = CreateAuthService(dbName);

        var request = new RegisterRequestDto
        {
            PhoneNumber = "01012345678",
            Username = "TestUser",
            Password = "aaaaaaa1",
            ConsentToAccountCreationAndDataCollection = true,
            ConsentToProductImprovementDataUsage = true,
            ConsentDocumentVersion = "v1.0"
        };

        var result = await authService.RegisterAsync(request, "127.0.0.1");

        Assert.False(result.IsSuccess);
        Assert.Equal(400, result.StatusCode);
        Assert.Equal(AuthErrorCode.WeakPassword, result.ErrorCode);
    }

    [Fact]
    public async Task Register_SmsDeliveryFailure_ShouldKeepAccountAsUnverifiedForSafeRetry()
    {
        var dbName = Guid.NewGuid().ToString();
        var (dbContext, userManager) = TestAuthDbContextFactory.Create(dbName);
        var authService = CreateAuthService(dbName);

        _smsSender.ShouldFail = true;

        var request = new RegisterRequestDto
        {
            PhoneNumber = "01099999999",
            Username = "Retry_User",
            Password = "Password123",
            ConsentToAccountCreationAndDataCollection = true,
            ConsentToProductImprovementDataUsage = true,
            ConsentDocumentVersion = "v1.0"
        };

        var result = await authService.RegisterAsync(request, "127.0.0.1");

        Assert.True(result.IsSuccess);
        Assert.Equal(201, result.StatusCode);
        Assert.Contains("تعذر إرسال", result.Message);

        var user = await userManager.FindByNameAsync("Retry_User");
        Assert.NotNull(user);
        Assert.False(user.PhoneNumberConfirmed);
        Assert.False(user.IsActive);
    }

    [Fact]
    public async Task Register_PasswordMismatch_ShouldReject()
    {
        var dbName = Guid.NewGuid().ToString();
        var authService = CreateAuthService(dbName);

        var request = new RegisterRequestDto
        {
            PhoneNumber = "01012345678",
            Username = "MismatchUser",
            Password = "Password123",
            ConfirmPassword = "DifferentPassword999"
        };

        var result = await authService.RegisterAsync(request, "127.0.0.1");

        Assert.False(result.IsSuccess);
        Assert.Equal(400, result.StatusCode);
        Assert.Equal(AuthErrorCode.PasswordMismatch, result.ErrorCode);
    }

    [Fact]
    public async Task Register_WithoutExplicitConsents_ShouldUseDefaultsAndSucceed()
    {
        var dbName = Guid.NewGuid().ToString();
        var authService = CreateAuthService(dbName);

        var request = new RegisterRequestDto
        {
            PhoneNumber = "01012345678",
            Username = "DefaultConsentUser",
            Password = "Password123",
            ConfirmPassword = "Password123"
        };

        var result = await authService.RegisterAsync(request, "127.0.0.1");

        Assert.True(result.IsSuccess);
        Assert.Equal(201, result.StatusCode);
    }
}

