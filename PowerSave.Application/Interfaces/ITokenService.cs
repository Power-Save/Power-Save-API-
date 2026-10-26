using PowerSave.Domain.Entities;

namespace PowerSave.Application.Interfaces;

public interface ITokenService
{
    string GenerateToken(ApplicationUser user);
    int ExpirationSeconds { get; }
}

