using System.Security.Cryptography;
using System.Text;
using PowerSave.Application.Interfaces;

namespace PowerSave.Infrastructure.Security;

public class OtpService : IOtpService
{
    private const int SaltSize = 16;
    private const int HashSize = 32;
    private const int Iterations = 10_000;

    public string GenerateSecureOtp()
    {

        int code = RandomNumberGenerator.GetInt32(100_000, 1_000_000);
        return code.ToString("D6");
    }

    public (string HashedOtp, string Salt) HashOtp(string otp)
    {
        byte[] saltBytes = RandomNumberGenerator.GetBytes(SaltSize);
        byte[] hashBytes = Rfc2898DeriveBytes.Pbkdf2(
            Encoding.UTF8.GetBytes(otp),
            saltBytes,
            Iterations,
            HashAlgorithmName.SHA256,
            HashSize);

        string salt = Convert.ToBase64String(saltBytes);
        string hashedOtp = Convert.ToBase64String(hashBytes);

        return (hashedOtp, salt);
    }

    public bool VerifyOtp(string inputOtp, string storedHashedOtp, string salt)
    {
        if (string.IsNullOrWhiteSpace(inputOtp) || string.IsNullOrWhiteSpace(storedHashedOtp) || string.IsNullOrWhiteSpace(salt))
            return false;

        try
        {
            byte[] saltBytes = Convert.FromBase64String(salt);
            byte[] expectedHashBytes = Convert.FromBase64String(storedHashedOtp);

            byte[] actualHashBytes = Rfc2898DeriveBytes.Pbkdf2(
                Encoding.UTF8.GetBytes(inputOtp),
                saltBytes,
                Iterations,
                HashAlgorithmName.SHA256,
                HashSize);

            return CryptographicOperations.FixedTimeEquals(actualHashBytes, expectedHashBytes);
        }
        catch
        {
            return false;
        }
    }
}

