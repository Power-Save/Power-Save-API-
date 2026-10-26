using Microsoft.Extensions.DependencyInjection;
using PowerSave.Application.Common.Interfaces;
using PowerSave.Application.Common.Services;
using PowerSave.Application.Interfaces;
using PowerSave.Application.Services;
using PowerSave.Application.Validation;

namespace PowerSave.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddSingleton<IDateTimeProvider, SystemDateTimeProvider>();

        services.AddScoped<IPhoneNumberValidator, PhoneNumberValidator>();
        services.AddScoped<IUsernameValidator, UsernameValidator>();
        services.AddScoped<IPasswordPolicyValidator, PasswordPolicyValidator>();
        services.AddScoped<IConsentPolicyValidator, ConsentPolicyValidator>();

        services.AddScoped<IAuthService, AuthService>();

        return services;
    }
}

