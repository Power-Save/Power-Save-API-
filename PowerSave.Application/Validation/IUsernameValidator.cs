namespace PowerSave.Application.Validation;

public interface IUsernameValidator
{
    bool Validate(string? username, out string? errorMessage);
}

