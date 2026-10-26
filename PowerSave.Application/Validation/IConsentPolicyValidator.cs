namespace PowerSave.Application.Validation;

public interface IConsentPolicyValidator
{
    string CurrentDocumentVersion { get; }
    string CurrentProductImprovementPolicyVersion { get; }
    bool Validate(bool consentToAccountCreation, bool consentToProductImprovement, string? documentVersion, out string? errorMessage);
}

