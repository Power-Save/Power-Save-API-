using System.Text.RegularExpressions;
using PowerSave.Application.Common.Localization;

namespace PowerSave.Application.Validation;

public class PasswordPolicyValidator : IPasswordPolicyValidator
{
    private static readonly HashSet<string> CommonPasswords = new(StringComparer.OrdinalIgnoreCase)
    {
        "password",
        "password1",
        "12345678",
        "123456789",
        "qwerty123",
        "admin123",
        "welcome1",
        "iloveyou1",
        "abc12345",
        "pass1234",
        "power123",
        "powersave1",
        "egypt123",
        "cairo123",
        "alex1234",
        "secret123"
    };

    public bool Validate(string? password, out string? errorMessage)
    {
        errorMessage = null;

        if (string.IsNullOrWhiteSpace(password))
        {
            errorMessage = AuthMessages.GetArabicMessage(AuthErrorCode.RequiredFieldMissing);
            return false;
        }

        if (password.Contains(' '))
        {
            errorMessage = AuthMessages.GetArabicMessage(AuthErrorCode.PasswordContainsSpaces);
            return false;
        }

        if (password.Length < 8)
        {
            errorMessage = AuthMessages.GetArabicMessage(AuthErrorCode.PasswordTooShort);
            return false;
        }

        bool hasLetter = password.Any(c => (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z'));

        bool hasDigit = password.Any(char.IsDigit);

        if (!hasLetter || !hasDigit)
        {
            errorMessage = AuthMessages.GetArabicMessage(AuthErrorCode.PasswordMissingLetterOrDigit);
            return false;
        }

        if (CommonPasswords.Contains(password))
        {
            errorMessage = AuthMessages.GetArabicMessage(AuthErrorCode.WeakPassword);
            return false;
        }

        var letters = password.Where(c => (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z')).Select(char.ToLowerInvariant).ToList();
        if (letters.Count >= 2 && letters.Distinct().Count() == 1)
        {

            errorMessage = AuthMessages.GetArabicMessage(AuthErrorCode.WeakPassword);
            return false;
        }

        if (password.Distinct().Count() <= 2)
        {
            errorMessage = AuthMessages.GetArabicMessage(AuthErrorCode.WeakPassword);
            return false;
        }

        return true;
    }
}

