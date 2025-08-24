using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace ScoreManagerForSchool.Core.Security
{
    public static class CryptoUtil
    {
        // 派生密钥：使用 PBKDF2 (Rfc2898) 返回指定长度的 keyBytes
        public static byte[] DeriveKey(string password, byte[] salt, int sizeBytes = 32, int iterations = 100_000)
        {
            byte[]? pwdBytes = null;
            try
            {
                pwdBytes = Encoding.UTF8.GetBytes(password);
                using var derive = new Rfc2898DeriveBytes(pwdBytes, salt ?? Array.Empty<byte>(), iterations, HashAlgorithmName.SHA256);
                var result = derive.GetBytes(sizeBytes);
                return result;
            }
            finally
            {
                if (pwdBytes != null)
                {
                    Array.Clear(pwdBytes, 0, pwdBytes.Length);
                }
            }
        }

        // Overload: derive key directly from char span to avoid creating a managed string when possible
        public static byte[] DeriveKey(ReadOnlySpan<char> passwordChars, byte[] salt, int sizeBytes = 32, int iterations = 100_000)
        {
            var byteCount = Encoding.UTF8.GetByteCount(passwordChars);
            using var buf = new SecurePinnedBuffer(byteCount);
            try
            {
                Encoding.UTF8.GetBytes(passwordChars, buf.Buffer);
                using var derive = new Rfc2898DeriveBytes(buf.Buffer, salt ?? Array.Empty<byte>(), iterations, HashAlgorithmName.SHA256);
                var result = derive.GetBytes(sizeBytes);
                return result;
            }
            finally
            {
                // SecurePinnedBuffer clears itself on Dispose
            }
        }

        // 确保 keyBytes 长度为 sizeBytes（不足补0，超出截断）
        private static byte[] NormalizeKeyBytes(byte[] keyBytes, int sizeBytes)
        {
            if (keyBytes == null) return new byte[sizeBytes];
            if (keyBytes.Length == sizeBytes) return keyBytes;
            var outBytes = new byte[sizeBytes];
            Array.Clear(outBytes, 0, outBytes.Length);
            Buffer.BlockCopy(keyBytes, 0, outBytes, 0, Math.Min(keyBytes.Length, sizeBytes));
            return outBytes;
        }

        // Encrypt plainText to Base64. keyBytes can be any length; it'll be normalized.
        public static string EncryptToBase64(string plainText, byte[] keyBytes)
        {
            var key = NormalizeKeyBytes(keyBytes, 32);
            try
            {
                using var aes = Aes.Create();
                aes.Key = key;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;
                aes.GenerateIV();
                using var ms = new MemoryStream();
                ms.Write(aes.IV, 0, aes.IV.Length);
                using var cs = new CryptoStream(ms, aes.CreateEncryptor(), CryptoStreamMode.Write);
                var plain = Encoding.UTF8.GetBytes(plainText);
                try
                {
                    cs.Write(plain, 0, plain.Length);
                    cs.FlushFinalBlock();
                }
                finally
                {
                    if (plain != null) Array.Clear(plain, 0, plain.Length);
                }
                return Convert.ToBase64String(ms.ToArray());
            }
            finally
            {
                // clear normalized key copy
                if (key != null) Array.Clear(key, 0, key.Length);
            }
        }

        // Overload: encrypt plain text provided as char span to avoid intermediate string where possible
        public static string EncryptToBase64(ReadOnlySpan<char> plainChars, byte[] keyBytes)
        {
            var key = NormalizeKeyBytes(keyBytes, 32);
            try
            {
                using var aes = Aes.Create();
                aes.Key = key;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;
                aes.GenerateIV();
                using var ms = new MemoryStream();
                ms.Write(aes.IV, 0, aes.IV.Length);
                using var cs = new CryptoStream(ms, aes.CreateEncryptor(), CryptoStreamMode.Write);
                var byteCount = Encoding.UTF8.GetByteCount(plainChars);
                using var buf = new SecurePinnedBuffer(byteCount);
                try
                {
                    Encoding.UTF8.GetBytes(plainChars, buf.Buffer);
                    cs.Write(buf.Buffer, 0, buf.Buffer.Length);
                    cs.FlushFinalBlock();
                }
                finally
                {
                    // SecurePinnedBuffer.Dispose will clear buffer
                }
                return Convert.ToBase64String(ms.ToArray());
            }
            finally
            {
                if (key != null) Array.Clear(key, 0, key.Length);
            }
        }

        // Decrypt Base64 cipher using keyBytes
        public static string DecryptFromBase64(string cipherBase64, byte[] keyBytes)
        {
            var data = Convert.FromBase64String(cipherBase64);
            var key = NormalizeKeyBytes(keyBytes, 32);
            try
            {
                using var aes = Aes.Create();
                aes.Key = key;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;
                using var ms = new MemoryStream(data);
                var iv = new byte[16];
                ms.Read(iv, 0, iv.Length);
                aes.IV = iv;
                using var cs = new CryptoStream(ms, aes.CreateDecryptor(), CryptoStreamMode.Read);
                using var sr = new StreamReader(cs, Encoding.UTF8);
                var result = sr.ReadToEnd();
                return result;
            }
            finally
            {
                if (key != null) Array.Clear(key, 0, key.Length);
            }
        }
    }
}
