using PowerSave.Application.Common.Localization;

namespace PowerSave.Application.Validation;

public class ConsentPolicyValidator : IConsentPolicyValidator
{
    public string CurrentDocumentVersion => "v1.0";
    public string CurrentProductImprovementPolicyVersion => "v1.0-improvement";

    public bool Validate(bool consentToAccountCreation, bool consentToProductImprovement, string? documentVersion, out string? errorMessage)
    {
        errorMessage = null;

        if (!consentToAccountCreation || !consentToProductImprovement)
        {
            errorMessage = AuthMessages.GetArabicMessage(AuthErrorCode.MissingRequiredConsent);
            return false;
        }

        if (string.IsNullOrWhiteSpace(documentVersion) || !string.Equals(documentVersion.Trim(), CurrentDocumentVersion, StringComparison.OrdinalIgnoreCase))
        {
            errorMessage = AuthMessages.GetArabicMessage(AuthErrorCode.UnsupportedConsentVersion);
            return false;
        }

        return true;
    }
}

