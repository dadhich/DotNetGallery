using System;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;

namespace ModernGallery.Security
{
    public class PasswordHasher
    {
        private const int IterationCount = 10000;
        private const int SaltSize = 16; // 128 bits
        private const int HashSize = 32; // 256 bits
        
        public static string HashPassword(string password)
        {
            // Generate a random salt
            byte[] salt = new byte[SaltSize];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(salt);
            }
            
            // Derive a 256-bit subkey (use HMACSHA256 with 10,000 iterations)
            byte[] hash = KeyDerivation.Pbkdf2(
                password: password,
                salt: salt,
                prf: KeyDerivationPrf.HMACSHA256,
                iterationCount: IterationCount,
                numBytesRequested: HashSize);
            
            // Format: {algorithm}${iterations}${salt}${hash}
            return $"PBKDF2$HMACSHA256${IterationCount}${Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}";
        }
        
        public static bool VerifyPassword(string hashedPassword, string providedPassword)
        {
            // Extract values from stored hash
            var parts = hashedPassword.Split('$');
            if (parts.Length != 5)
            {
                return false; // Invalid hash format
            }
            
            var algorithm = parts[0];
            var prf = parts[1];
            var iterCount = int.Parse(parts[2]);
            var salt = Convert.FromBase64String(parts[3]);
            var storedHash = Convert.FromBase64String(parts[4]);
            
            // Verify algorithm and PRF
            if (algorithm != "PBKDF2" || prf != "HMACSHA256")
            {
                return false;
            }
            
            // Compute hash of provided password
            byte[] computedHash = KeyDerivation.Pbkdf2(
                password: providedPassword,
                salt: salt,
                prf: KeyDerivationPrf.HMACSHA256,
                iterationCount: iterCount,
                numBytesRequested: storedHash.Length);
            
            // Time-constant comparison to avoid timing attacks
            return CryptographicOperations.FixedTimeEquals(storedHash, computedHash);
        }
    }
}