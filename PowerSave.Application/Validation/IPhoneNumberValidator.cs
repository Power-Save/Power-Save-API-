namespace PowerSave.Application.Validation;

public interface IPhoneNumberValidator
{
    bool Validate(string? rawPhoneNumber, out string normalizedPhoneNumber, out string? errorMessage);
}

