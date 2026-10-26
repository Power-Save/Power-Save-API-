using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using PowerSave.Application.Common.Localization;
using PowerSave.Application.DTOs;
using PowerSave.Application.Services;
using PowerSave.Application.Validation;
using PowerSave.Domain.Entities;
using PowerSave.Infrastructure.Persistence;
using PowerSave.Infrastructure.Security;
using PowerSave.Tests.Helpers;
using Xunit;

namespace PowerSave.Tests.Authentication;

public class LoginTests
{
    private readonly TestTimeProvider _timeProvider = new();
    private readonly TestSmsSender _smsSender = new();
    private readonly PhoneNumberValidator _phoneValidator = new();
    private readonly UsernameValidator _usernameValidator = new();
    private readonly PasswordPolicyValidator _passwordValidator = new();
    private readonly ConsentPolicyValidator _consentValidator = new();
    private readonly OtpService _otpService = new();

    private (AuthService AuthService, PowerSaveDbContext DbContext, UserManager<ApplicationUser> UserManager) CreateAuthService(string dbName)
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

        var service = new AuthService(
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

        return (service, dbContext, userManager);
    }

    [Fact]
    public async Task Login_ValidCredentialsAfterVerification_ShouldSucceedWithToken()
    {
        var dbName = Guid.NewGuid().ToString();
        var (authService, _, _) = CreateAuthService(dbName);

        await authService.RegisterAsync(new RegisterRequestDto
        {
            PhoneNumber = "01012345678",
            Username = "LoginUser",
            Password = "SecurePassword1",
            ConsentToAccountCreationAndDataCollection = true,
            ConsentToProductImprovementDataUsage = true,
            ConsentDocumentVersion = "v1.0"
        }, "127.0.0.1");

        var otpCode = _smsSender.LastExtractedOtp!;

        await authService.VerifyPhoneAsync(new VerifyPhoneRequestDto
        {
            Identifier = "LoginUser",
            OtpCode = otpCode
        });

        var loginResult = await authService.LoginAsync(new LoginRequestDto
        {
            Identifier = "LoginUser",
            Password = "SecurePassword1"
        });

        Assert.True(loginResult.IsSuccess);
        Assert.Equal(200, loginResult.StatusCode);
        Assert.NotNull(loginResult.Data);
        Assert.NotEmpty(loginResult.Data.AccessToken);
        Assert.Equal("Bearer", loginResult.Data.TokenType);
        Assert.Equal("LoginUser", loginResult.Data.User.Username);

        var phoneLoginResult = await authService.LoginAsync(new LoginRequestDto
        {
            Identifier = "01012345678",
            Password = "SecurePassword1"
        });

        Assert.True(phoneLoginResult.IsSuccess);
        Assert.NotEmpty(phoneLoginResult.Data!.AccessToken);
    }

    [Fact]
    public async Task Login_BeforePhoneVerification_ShouldRejectWith403()
    {
        var dbName = Guid.NewGuid().ToString();
        var (authService, _, _) = CreateAuthService(dbName);

        await authService.RegisterAsync(new RegisterRequestDto
        {
            PhoneNumber = "01012345678",
            Username = "UnverifiedUser",
            Password = "SecurePassword1",
            ConsentToAccountCreationAndDataCollection = true,
            ConsentToProductImprovementDataUsage = true,
            ConsentDocumentVersion = "v1.0"
        }, "127.0.0.1");

        var loginResult = await authService.LoginAsync(new LoginRequestDto
        {
            Identifier = "UnverifiedUser",
            Password = "SecurePassword1"
        });

        Assert.False(loginResult.IsSuccess);
        Assert.Equal(403, loginResult.StatusCode);
        Assert.Equal(AuthErrorCode.AccountPhoneUnverified, loginResult.ErrorCode);
    }

    [Fact]
    public async Task Login_IncorrectCredentials_ShouldReturnGeneric401WithoutEnumeration()
    {
        var dbName = Guid.NewGuid().ToString();
        var (authService, _, _) = CreateAuthService(dbName);

        var nonExistentResult = await authService.LoginAsync(new LoginRequestDto
        {
            Identifier = "NonExistentUser_999",
            Password = "SomePassword1"
        });

        Assert.False(nonExistentResult.IsSuccess);
        Assert.Equal(401, nonExistentResult.StatusCode);
        Assert.Equal(AuthErrorCode.InvalidCredentials, nonExistentResult.ErrorCode);

        await authService.RegisterAsync(new RegisterRequestDto
        {
            PhoneNumber = "01012345678",
            Username = "ExistingUser",
            Password = "CorrectPassword1",
            ConsentToAccountCreationAndDataCollection = true,
            ConsentToProductImprovementDataUsage = true,
            ConsentDocumentVersion = "v1.0"
        }, "127.0.0.1");

        await authService.VerifyPhoneAsync(new VerifyPhoneRequestDto
        {
            Identifier = "ExistingUser",
            OtpCode = _smsSender.LastExtractedOtp!
        });

        var wrongPasswordResult = await authService.LoginAsync(new LoginRequestDto
        {
            Identifier = "ExistingUser",
            Password = "WrongPassword999"
        });

        Assert.False(wrongPasswordResult.IsSuccess);
        Assert.Equal(401, wrongPasswordResult.StatusCode);
        Assert.Equal(AuthErrorCode.InvalidCredentials, wrongPasswordResult.ErrorCode);

        Assert.Equal(nonExistentResult.Message, wrongPasswordResult.Message);
    }

    [Fact]
    public async Task Login_InactiveAccount_ShouldRejectWith403()
    {
        var dbName = Guid.NewGuid().ToString();
        var (authService, dbContext, userManager) = CreateAuthService(dbName);

        await authService.RegisterAsync(new RegisterRequestDto
        {
            PhoneNumber = "01012345678",
            Username = "DeactivatedUser",
            Password = "SecurePassword1",
            ConsentToAccountCreationAndDataCollection = true,
            ConsentToProductImprovementDataUsage = true,
            ConsentDocumentVersion = "v1.0"
        }, "127.0.0.1");

        await authService.VerifyPhoneAsync(new VerifyPhoneRequestDto
        {
            Identifier = "DeactivatedUser",
            OtpCode = _smsSender.LastExtractedOtp!
        });

        var user = await userManager.FindByNameAsync("DeactivatedUser");
        Assert.NotNull(user);
        user.IsActive = false;
        await userManager.UpdateAsync(user);

        var loginResult = await authService.LoginAsync(new LoginRequestDto
        {
            Identifier = "DeactivatedUser",
            Password = "SecurePassword1"
        });

        Assert.False(loginResult.IsSuccess);
        Assert.Equal(403, loginResult.StatusCode);
        Assert.Equal(AuthErrorCode.AccountInactive, loginResult.ErrorCode);
    }

}

