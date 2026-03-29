using System.Security.Cryptography;
using System.Text;

namespace GymManagement.Api.Services;

public static class PasswordHasher
{
    public static string Hash(string value)
    {
        var bytes = Encoding.UTF8.GetBytes(value);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash);
    }
}
