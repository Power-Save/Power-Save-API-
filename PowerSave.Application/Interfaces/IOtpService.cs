namespace PowerSave.Application.Interfaces;

public interface IOtpService
{
    string GenerateSecureOtp();
    (string HashedOtp, string Salt) HashOtp(string otp);
    bool VerifyOtp(string inputOtp, string storedHashedOtp, string salt);
}

