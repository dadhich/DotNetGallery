// Security/Encryption.cs - Data encryption for sensitive information
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace ModernGallery.Security
{
    public class Encryption
    {
        private static readonly byte[] Key = GenerateKey();
        private static readonly int BlockSize = 16;
        
        private static byte[] GenerateKey()
        {
            // In a real application, this would be securely stored or derived
            // For this example, we're using a hardcoded key for simplicity
            return new byte[]
            {
                0x01, 0x23, 0x45, 0x67, 0x89, 0xAB, 0xCD, 0xEF,
                0xFE, 0xDC, 0xBA, 0x98, 0x76, 0x54, 0x32, 0x10,
                0x10, 0x32, 0x54, 0x76, 0x98, 0xBA, 0xDC, 0xFE,
                0xEF, 0xCD, 0xAB, 0x89, 0x67, 0x45, 0x23, 0x01
            };
        }
        
        public static string EncryptString(string plainText)
        {
            if (string.IsNullOrEmpty(plainText))
            {
                return plainText;
            }
            
            byte[] iv = new byte[BlockSize];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(iv);
            }
            
            using (var aes = Aes.Create())
            {
                aes.Key = Key;
                aes.IV = iv;
                
                using (var encryptor = aes.CreateEncryptor(aes.Key, aes.IV))
                using (var ms = new MemoryStream())
                {
                    // Write IV to the beginning of the stream
                    ms.Write(iv, 0, iv.Length);
                    
                    using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                    using (var sw = new StreamWriter(cs))
                    {
                        sw.Write(plainText);
                    }
                    
                    return Convert.ToBase64String(ms.ToArray());
                }
            }
        }
        
        public static string DecryptString(string cipherText)
        {
            if (string.IsNullOrEmpty(cipherText))
            {
                return cipherText;
            }
            
            try
            {
                byte[] cipherBytes = Convert.FromBase64String(cipherText);
                
                // Extract IV from the beginning of the ciphertext
                byte[] iv = new byte[BlockSize];
                Array.Copy(cipherBytes, iv, iv.Length);
                
                using (var aes = Aes.Create())
                {
                    aes.Key = Key;
                    aes.IV = iv;
                    
                    using (var decryptor = aes.CreateDecryptor(aes.Key, aes.IV))
                    using (var ms = new MemoryStream(cipherBytes, iv.Length, cipherBytes.Length - iv.Length))
                    using (var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
                    using (var sr = new StreamReader(cs))
                    {
                        return sr.ReadToEnd();
                    }
                }
            }
            catch
            {
                // In a production application, you would log this error
                return string.Empty;
            }
        }
    }
}