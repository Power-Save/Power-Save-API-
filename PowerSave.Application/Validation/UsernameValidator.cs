using System.Text.RegularExpressions;
using PowerSave.Application.Common.Localization;

namespace PowerSave.Application.Validation;

public class UsernameValidator : IUsernameValidator
{

    private static readonly Regex AllowedCharsRegex = new(@"^[a-zA-Z0-9#_!]+$", RegexOptions.Compiled);

    public bool Validate(string? username, out string? errorMessage)
    {
        errorMessage = null;

        if (string.IsNullOrWhiteSpace(username))
        {
            errorMessage = AuthMessages.GetArabicMessage(AuthErrorCode.RequiredFieldMissing);
            return false;
        }

        if (username.Length < 3 || username.Length > 20)
        {
            errorMessage = AuthMessages.GetArabicMessage(AuthErrorCode.InvalidUsernameLength);
            return false;
        }

        if (username.Contains(' ') || !AllowedCharsRegex.IsMatch(username))
        {
            errorMessage = AuthMessages.GetArabicMessage(AuthErrorCode.UnsupportedUsernameCharacters);
            return false;
        }

        return true;
    }
}

