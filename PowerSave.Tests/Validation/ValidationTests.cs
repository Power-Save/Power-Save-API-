using PowerSave.Application.Validation;
using Xunit;

namespace PowerSave.Tests.Validation;

public class ValidationTests
{
    private readonly PhoneNumberValidator _phoneValidator = new();
    private readonly UsernameValidator _usernameValidator = new();
    private readonly PasswordPolicyValidator _passwordValidator = new();
    private readonly ConsentPolicyValidator _consentValidator = new();

    #region Egyptian Mobile Validation Tests

    [Theory]
    [InlineData("01012345678", "01012345678")]
    [InlineData("01123456789", "01123456789")]
    [InlineData("01234567890", "01234567890")]
    [InlineData("01555555555", "01555555555")]
    public void PhoneNumberValidator_Valid11Digits_ShouldSucceedAndNormalize(string input, string expected)
    {
        var isValid = _phoneValidator.Validate(input, out var normalized, out var error);
        Assert.True(isValid);
        Assert.Equal(expected, normalized);
        Assert.Null(error);
    }

    [Theory]
    [InlineData("1012345678", "01012345678")]
    [InlineData("1123456789", "01123456789")]
    [InlineData("1234567890", "01234567890")]
    [InlineData("1555555555", "01555555555")]
    public void PhoneNumberValidator_Valid10Digits_ShouldSucceedAndPrependZero(string input, string expected)
    {
        var isValid = _phoneValidator.Validate(input, out var normalized, out var error);
        Assert.True(isValid);
        Assert.Equal(expected, normalized);
        Assert.Null(error);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("01312345678")]
    [InlineData("01412345678")]
    [InlineData("01912345678")]
    [InlineData("010123456")]
    [InlineData("010123456789")]
    [InlineData("0101234a678")]
    [InlineData("010 1234567")]
    [InlineData("1312345678")]
    public void PhoneNumberValidator_InvalidInputs_ShouldFail(string input)
    {
        var isValid = _phoneValidator.Validate(input, out var normalized, out var error);
        Assert.False(isValid);
        Assert.Empty(normalized);
        Assert.NotNull(error);
    }

    #endregion

    #region Username Validation Tests

    [Theory]
    [InlineData("Mohamed_10")]
    [InlineData("Ali#123")]
    [InlineData("Sara!")]
    [InlineData("user_name")]
    [InlineData("abc")]
    [InlineData("12345678901234567890")]
    public void UsernameValidator_ValidUsernames_ShouldSucceed(string username)
    {
        var isValid = _usernameValidator.Validate(username, out var error);
        Assert.True(isValid);
        Assert.Null(error);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("ab")]
    [InlineData("123456789012345678901")]
    [InlineData("mohamed 10")]
    [InlineData("mohamed@10")]
    [InlineData("user$name")]
    [InlineData("محمد")]
    public void UsernameValidator_InvalidUsernames_ShouldFail(string username)
    {
        var isValid = _usernameValidator.Validate(username, out var error);
        Assert.False(isValid);
        Assert.NotNull(error);
    }

    #endregion

    #region Password Validation Tests

    [Theory]
    [InlineData("StrongSecret1")]
    [InlineData("Pass_word1")]

    [InlineData("power100Save")]
    [InlineData("validP@ssw0rd")]
    [InlineData("lowercase1")]
    public void PasswordPolicyValidator_ValidPasswords_ShouldSucceed(string password)
    {
        var isValid = _passwordValidator.Validate(password, out var error);
        Assert.True(isValid);
        Assert.Null(error);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("12345678")]
    [InlineData("abcdefgh")]
    [InlineData("Abc1")]
    [InlineData("Pass word1")]
    [InlineData("Pass  word12")]
    public void PasswordPolicyValidator_StructuralRequirements_ShouldFail(string password)
    {
        var isValid = _passwordValidator.Validate(password, out var error);
        Assert.False(isValid);
        Assert.NotNull(error);
    }

    [Theory]
    [InlineData("aaaaaaa1")]
    [InlineData("1aaaaaaa")]
    [InlineData("bbbbbbb2")]
    [InlineData("1111111a")]
    [InlineData("xxxxxxx9")]
    public void PasswordPolicyValidator_RepeatedCharacterWeakPasswords_ShouldFail(string password)
    {
        var isValid = _passwordValidator.Validate(password, out var error);
        Assert.False(isValid);
        Assert.NotNull(error);
        Assert.Contains("ضعيفة", error);
    }

    [Theory]
    [InlineData("password")]
    [InlineData("password1")]
    [InlineData("12345678")]
    [InlineData("admin123")]
    [InlineData("qwerty123")]
    [InlineData("welcome1")]
    public void PasswordPolicyValidator_CommonPasswords_ShouldFail(string password)
    {
        var isValid = _passwordValidator.Validate(password, out var error);
        Assert.False(isValid);
        Assert.NotNull(error);
    }

    #endregion

    #region Consent Policy Tests

    [Fact]
    public void ConsentPolicyValidator_AllConsentsAndValidVersion_ShouldSucceed()
    {
        var isValid = _consentValidator.Validate(
            consentToAccountCreation: true,
            consentToProductImprovement: true,
            documentVersion: "v1.0",
            out var error);

        Assert.True(isValid);
        Assert.Null(error);
    }

    [Theory]
    [InlineData(false, true, "v1.0")]
    [InlineData(true, false, "v1.0")]
    [InlineData(false, false, "v1.0")]
    public void ConsentPolicyValidator_MissingConsentFlags_ShouldFail(bool consent1, bool consent2, string version)
    {
        var isValid = _consentValidator.Validate(consent1, consent2, version, out var error);
        Assert.False(isValid);
        Assert.NotNull(error);
        Assert.Contains("الموافقة الإلزامية", error);
    }

    [Theory]
    [InlineData("v2.0")]
    [InlineData("")]
    [InlineData("v0.9")]
    [InlineData("1.0")]
    public void ConsentPolicyValidator_UnsupportedVersion_ShouldFail(string version)
    {
        var isValid = _consentValidator.Validate(true, true, version, out var error);
        Assert.False(isValid);
        Assert.NotNull(error);
        Assert.Contains("إصدار وثيقة الموافقة غير معتمد", error);
    }

    #endregion
}

