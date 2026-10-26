using System.Text.RegularExpressions;
using PowerSave.Application.Common.Localization;

namespace PowerSave.Application.Validation;

public class PhoneNumberValidator : IPhoneNumberValidator
{

    private static readonly Regex ElevenDigitRegex = new(@"^01[0125]\d{8}$", RegexOptions.Compiled);
    private static readonly Regex TenDigitRegex = new(@"^1[0125]\d{8}$", RegexOptions.Compiled);

    public bool Validate(string? rawPhoneNumber, out string normalizedPhoneNumber, out string? errorMessage)
    {
        normalizedPhoneNumber = string.Empty;
        errorMessage = null;

        if (string.IsNullOrWhiteSpace(rawPhoneNumber))
        {
            errorMessage = AuthMessages.GetArabicMessage(AuthErrorCode.RequiredFieldMissing);
            return false;
        }

        var trimmed = rawPhoneNumber.Trim();

        if (!trimmed.All(char.IsDigit))
        {
            errorMessage = AuthMessages.GetArabicMessage(AuthErrorCode.InvalidPhoneNumber);
            return false;
        }

        if (trimmed.Length == 11)
        {
            if (ElevenDigitRegex.IsMatch(trimmed))
            {
                normalizedPhoneNumber = trimmed;
                return true;
            }
        }
        else if (trimmed.Length == 10)
        {
            if (TenDigitRegex.IsMatch(trimmed))
            {
                normalizedPhoneNumber = "0" + trimmed;
                return true;
            }
        }

        errorMessage = AuthMessages.GetArabicMessage(AuthErrorCode.InvalidPhoneNumber);
        return false;
    }
}

