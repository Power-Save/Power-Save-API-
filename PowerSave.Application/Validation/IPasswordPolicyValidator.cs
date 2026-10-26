namespace PowerSave.Application.Validation;

public interface IPasswordPolicyValidator
{
    bool Validate(string? password, out string? errorMessage);
}

