using System;
using System.Security.Cryptography;
using System.Text;
using System.IO;

namespace KSO.Modules
{
    public static class EncryptionHelper
    {
        private static readonly byte[] Key = SHA256.HashData(Encoding.UTF8.GetBytes("KSO_Encryption_Key_123456_Secret"));
        private static readonly byte[] IV = MD5.HashData(Encoding.UTF8.GetBytes("KSO_IV_1234567890"))[..16];

        public static string Encrypt(string plainText)
        {
            if(string.IsNullOrEmpty(plainText)) return "";
            using var aes = Aes.Create();
            aes.Key = Key;
            aes.IV = IV;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;
            
            using var encryptor = aes.CreateEncryptor();
            using var ms = new MemoryStream();
            using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
            {
                var bytes = Encoding.UTF8.GetBytes(plainText);
                cs.Write(bytes, 0, bytes.Length);
            }
            return Convert.ToBase64String(ms.ToArray());
        }

        public static string Decrypt(string cipherText)
        {
            if(string.IsNullOrEmpty(cipherText)) return "";
            try
            {
                using var aes = Aes.Create();
                aes.Key = Key;
                aes.IV = IV;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                using var decryptor = aes.CreateDecryptor();
                var bytes = Convert.FromBase64String(cipherText);
                using var ms = new MemoryStream(bytes);
                using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
                using var sr = new StreamReader(cs, Encoding.UTF8);
                return sr.ReadToEnd();
            }
            catch { return cipherText; }
        }
    }
}