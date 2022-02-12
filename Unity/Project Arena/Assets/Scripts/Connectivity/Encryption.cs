using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Connectivity
{
    /// <summary>
    ///     Encryption utilities. Decryption is actually not required client-side.
    /// </summary>
    public class Encryption
    {
        public static string Encrypt(string messageString, string keyString, string ivString)
        {
            return EncryptRJ256(messageString, keyString, ivString);
        }

        public static string Decrypt(string messageString, string keyString, string ivString)
        {
            return DecryptRJ256(messageString, keyString, ivString);
        }

        public static string Hash(string message)
        {
            return HashMD5(message);
        }

        private static string EncryptRJ256(string messageString, string keyString, string ivString)
        {
            var message = Encoding.ASCII.GetBytes(messageString);
            var key = Encoding.ASCII.GetBytes(keyString);
            var iv = Encoding.ASCII.GetBytes(ivString);

            var algorithm = new RijndaelManaged
            {
                Padding = PaddingMode.Zeros,
                Mode = CipherMode.CBC,
                KeySize = 256,
                BlockSize = 256
            };

            using (var encryptor = algorithm.CreateEncryptor(key, iv))
            {
                using (var memoryStream = new MemoryStream())
                {
                    using (var cryptoStream = new CryptoStream(memoryStream, encryptor,
                        CryptoStreamMode.Write))
                    {
                        cryptoStream.Write(message, 0, message.Length);
                        cryptoStream.FlushFinalBlock();
                        var encrypted = memoryStream.ToArray();
                        return Convert.ToBase64String(encrypted);
                    }
                }
            }
        }

        private static string DecryptRJ256(string messageString, string keyString, string ivString)
        {
            var message = Convert.FromBase64String(messageString);
            var key = Encoding.ASCII.GetBytes(keyString);
            var iv = Encoding.ASCII.GetBytes(ivString);

            var algorithm = new RijndaelManaged
            {
                Padding = PaddingMode.Zeros,
                Mode = CipherMode.CBC,
                KeySize = 256,
                BlockSize = 256
            };

            var decryptor = algorithm.CreateDecryptor(key, iv);
            using (var memoryStream = new MemoryStream(message))
            {
                using (var cryptoStream = new CryptoStream(memoryStream, decryptor,
                    CryptoStreamMode.Read))
                {
                    var decrypted = new byte[message.Length];
                    cryptoStream.Read(decrypted, 0, decrypted.Length);
                    return Encoding.ASCII.GetString(decrypted);
                }
            }
        }

        private static string HashMD5(string message)
        {
            using (var md5 = MD5.Create())
            {
                var inputBytes = Encoding.UTF8.GetBytes(message);
                var hashBytes = md5.ComputeHash(inputBytes);
                var builder = new StringBuilder();
                for (var i = 0; i < hashBytes.Length; i++) builder.Append(hashBytes[i].ToString("x2"));
                return builder.ToString();
            }
        }
    }
}