using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using PowerSave.Application.DTOs;
using PowerSave.Infrastructure.Sms;
using Xunit;

namespace PowerSave.Tests.Integration;

public class EndToEndLiveApiTests : IClassFixture<WebApplicationFactory<Power_Save.Program>>
{
    private readonly WebApplicationFactory<Power_Save.Program> _factory;

    public EndToEndLiveApiTests(WebApplicationFactory<Power_Save.Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Development");
        });
    }

    [Fact]
    public async Task CompleteE2EFlow_Register_VerifyOtp_Login_AccessProtectedEndpoint()
    {
        var client = _factory.CreateClient();

        var randomDigits = Random.Shared.Next(10000000, 99999999).ToString();
        var testPhone = $"010{randomDigits}";
        var testUsername = $"User_{randomDigits}";

        var registerPayload = new RegisterRequestDto
        {
            Username = testUsername,
            PhoneNumber = testPhone,
            Password = "StrongPassword123!",
            ConfirmPassword = "StrongPassword123!",
            ConsentToAccountCreationAndDataCollection = true,
            ConsentToProductImprovementDataUsage = true,
            ConsentDocumentVersion = "v1.0"
        };

        var regHttpResponse = await client.PostAsJsonAsync("/api/auth/register", registerPayload);
        var regText = await regHttpResponse.Content.ReadAsStringAsync();
        Assert.True(regHttpResponse.StatusCode == HttpStatusCode.Created, $"Failed with: {regHttpResponse.StatusCode} - {regText}");

        var regBody = await regHttpResponse.Content.ReadFromJsonAsync<ApiResponse<RegisterResponseDto>>();
        Assert.NotNull(regBody);
        Assert.True(regBody.Success);
        Assert.Equal(testUsername, regBody.Data!.Username);
        Assert.Equal(testPhone, regBody.Data!.PhoneNumber);
        Assert.False(regBody.Data!.IsPhoneVerified);

        var prematureLoginPayload = new LoginRequestDto
        {
            Identifier = testUsername,
            Password = "StrongPassword123!"
        };

        var prematureLoginResponse = await client.PostAsJsonAsync("/api/auth/login", prematureLoginPayload);
        Assert.Equal(HttpStatusCode.Forbidden, prematureLoginResponse.StatusCode);

        var sentMsg = DevSmsSender.SentMessages.FirstOrDefault(m => m.PhoneNumber == testPhone);
        Assert.True(sentMsg != default, "DevSmsSender should have recorded an SMS message for this phone number");

        var otpMatch = Regex.Match(sentMsg.Message, @"\b\d{6}\b");
        Assert.True(otpMatch.Success, $"Could not find 6-digit OTP in message: {sentMsg.Message}");
        var otpCode = otpMatch.Value;

        var verifyPayload = new VerifyPhoneRequestDto
        {
            Identifier = testUsername,
            OtpCode = otpCode
        };

        var verifyResponse = await client.PostAsJsonAsync("/api/auth/verify-phone", verifyPayload);
        Assert.Equal(HttpStatusCode.OK, verifyResponse.StatusCode);

        var verifyBody = await verifyResponse.Content.ReadFromJsonAsync<ApiResponse<VerifyPhoneResponseDto>>();
        Assert.NotNull(verifyBody);
        Assert.True(verifyBody.Success);
        Assert.True(verifyBody.Data!.IsVerified);
        Assert.True(verifyBody.Data!.IsActive);

        var loginPayload = new LoginRequestDto
        {
            Identifier = testUsername,
            Password = "StrongPassword123!"
        };

        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", loginPayload);
        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);

        var loginBody = await loginResponse.Content.ReadFromJsonAsync<ApiResponse<LoginResponseDto>>();
        Assert.NotNull(loginBody);
        Assert.True(loginBody.Success);
        Assert.NotNull(loginBody.Data);
        Assert.False(string.IsNullOrWhiteSpace(loginBody.Data.AccessToken));
        Assert.True(loginBody.Data.User.IsPhoneVerified);
        Assert.Equal(testUsername, loginBody.Data.User.Username);

        var jwtToken = loginBody.Data.AccessToken;

        var phoneLoginPayload = new LoginRequestDto
        {
            Identifier = testPhone,
            Password = "StrongPassword123!"
        };

        var phoneLoginResponse = await client.PostAsJsonAsync("/api/auth/login", phoneLoginPayload);
        Assert.Equal(HttpStatusCode.OK, phoneLoginResponse.StatusCode);

        var unauthClient = _factory.CreateClient();
        var unauthResponse = await unauthClient.GetAsync("/api/auth/me");
        Assert.Equal(HttpStatusCode.Unauthorized, unauthResponse.StatusCode);

        var authClient = _factory.CreateClient();
        authClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwtToken);

        var authMeResponse = await authClient.GetAsync("/api/auth/me");
        Assert.Equal(HttpStatusCode.OK, authMeResponse.StatusCode);

        var authMeBody = await authMeResponse.Content.ReadFromJsonAsync<ApiResponse<UserProfileResponseDto>>();
        Assert.NotNull(authMeBody);
        Assert.True(authMeBody.Success);
        Assert.Equal(testUsername, authMeBody.Data!.Username);
        Assert.Equal(testPhone, authMeBody.Data!.PhoneNumber);
        Assert.True(authMeBody.Data!.IsPhoneVerified);
    }

    [Fact]
    public async Task Login_InvalidPassword_ShouldReturn401Unauthorized()
    {
        var client = _factory.CreateClient();

        var loginPayload = new LoginRequestDto
        {
            Identifier = "NonExistingOrWrongUser",
            Password = "WrongPassword999!"
        };

        var response = await client.PostAsJsonAsync("/api/auth/login", loginPayload);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}

