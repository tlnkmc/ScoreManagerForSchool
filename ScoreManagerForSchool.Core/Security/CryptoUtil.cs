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
            var outBytes = new byte[sizeBytes];
            if (keyBytes == null || keyBytes.Length == 0)
            {
                // already zeroed
                return outBytes;
            }
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
                var plain = Encoding.UTF8.GetBytes(plainText);
                try
                {
                    using var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
                    var cipher = encryptor.TransformFinalBlock(plain, 0, plain.Length);
                    var output = new byte[aes.IV.Length + cipher.Length];
                    Buffer.BlockCopy(aes.IV, 0, output, 0, aes.IV.Length);
                    Buffer.BlockCopy(cipher, 0, output, aes.IV.Length, cipher.Length);
                    return Convert.ToBase64String(output);
                }
                finally
                {
                    if (plain != null) Array.Clear(plain, 0, plain.Length);
                }
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
                var byteCount = Encoding.UTF8.GetByteCount(plainChars);
                using var buf = new SecurePinnedBuffer(byteCount);
                try
                {
                    Encoding.UTF8.GetBytes(plainChars, buf.Buffer);
                    using var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
                    var cipher = encryptor.TransformFinalBlock(buf.Buffer, 0, buf.Buffer.Length);
                    var output = new byte[aes.IV.Length + cipher.Length];
                    Buffer.BlockCopy(aes.IV, 0, output, 0, aes.IV.Length);
                    Buffer.BlockCopy(cipher, 0, output, aes.IV.Length, cipher.Length);
                    return Convert.ToBase64String(output);
                }
                finally
                {
                    // SecurePinnedBuffer.Dispose will clear buffer
                }
            }
            finally
            {
                if (key != null) Array.Clear(key, 0, key.Length);
            }
        }

        // Decrypt Base64 cipher using keyBytes
        public static string DecryptFromBase64(string cipherBase64, byte[] keyBytes)
        {
            if (string.IsNullOrEmpty(cipherBase64))
                throw new ArgumentException("密文不能为空", nameof(cipherBase64));
            
            if (keyBytes == null || keyBytes.Length == 0)
                throw new ArgumentException("密钥不能为空", nameof(keyBytes));

            byte[]? data = null;
            byte[]? key = null;
            
            try
            {
                data = Convert.FromBase64String(cipherBase64);
                
                // 详细的数据验证
                if (data.Length < 16)
                    throw new ArgumentException($"加密数据格式无效：长度不足 ({data.Length} < 16 字节)");
                
                // 检查数据长度是否合理（应该是16的倍数+16字节IV）
                if ((data.Length - 16) % 16 != 0)
                {
                    throw new ArgumentException($"加密数据格式可能无效：数据长度 {data.Length} 不符合AES块大小要求");
                }
                
                key = NormalizeKeyBytes(keyBytes, 32);
                
                using var aes = Aes.Create();
                aes.Key = key;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;
                
                // 拆分 IV 与密文
                var iv = new byte[16];
                Buffer.BlockCopy(data, 0, iv, 0, 16);
                var cipherLen = data.Length - 16;
                var cipher = new byte[cipherLen];
                Buffer.BlockCopy(data, 16, cipher, 0, cipherLen);
                aes.IV = iv;

                try
                {
                    using var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
                    var plain = decryptor.TransformFinalBlock(cipher, 0, cipher.Length);
                    var result = Encoding.UTF8.GetString(plain);
                    if (string.IsNullOrEmpty(result))
                        throw new CryptographicException("Decryption result is empty, possibly wrong key");
                    return result;
                }
                catch (CryptographicException ex) when (ex.Message.Contains("Padding is invalid", StringComparison.OrdinalIgnoreCase) || ex.Message.Contains("padding", StringComparison.OrdinalIgnoreCase))
                {
                    throw new CryptographicException(
                        "Decryption failed: Invalid padding. Possible reasons:\n" +
                        "1. Key is incorrect\n" +
                        "2. Encrypted data is corrupted\n" +
                        "3. Data format mismatch\n" +
                        $"Data length: {data.Length} bytes", ex);
                }
                catch (CryptographicException ex)
                {
                    throw new CryptographicException($"Decryption failed: {ex.Message}. Data length: {data.Length} bytes", ex);
                }
                catch (Exception ex)
                {
                    throw new CryptographicException($"Unknown error during decryption: {ex.Message}", ex);
                }
            }
            catch (FormatException ex)
            {
                throw new ArgumentException("Invalid Base64 format", nameof(cipherBase64), ex);
            }
            finally
            {
                if (key != null) Array.Clear(key, 0, key.Length);
                if (data != null) Array.Clear(data, 0, data.Length);
            }
        }

        /// <summary>
        /// 安全解密方法，包含错误恢复和诊断功能
        /// </summary>
        public static string SafeDecryptFromBase64(string cipherBase64, byte[] keyBytes, bool allowFallback = true)
        {
            if (string.IsNullOrEmpty(cipherBase64))
                throw new ArgumentException("Cipher text cannot be empty", nameof(cipherBase64));
            
            if (keyBytes == null || keyBytes.Length == 0)
                throw new ArgumentException("Key cannot be empty", nameof(keyBytes));

            // 首先尝试正常解密
            try
            {
                return DecryptFromBase64(cipherBase64, keyBytes);
            }
            catch (CryptographicException ex) when (allowFallback)
            {
                // 记录详细错误信息
                var diagnosis = DiagnoseEncryptedData(cipherBase64);
                System.Diagnostics.Debug.WriteLine($"Decryption failed, diagnostic info:\n{diagnosis}");
                System.Diagnostics.Debug.WriteLine($"Error details: {ex.Message}");
                
                // 尝试替代解密方法
                return TryAlternativeDecryption(cipherBase64, keyBytes);
            }
        }

        /// <summary>
        /// 尝试替代的解密方法
        /// </summary>
        private static string TryAlternativeDecryption(string cipherBase64, byte[] keyBytes)
        {
            byte[]? data = null;
            byte[]? key = null;
            
            try
            {
                data = Convert.FromBase64String(cipherBase64);
                key = NormalizeKeyBytes(keyBytes, 32);
                
                // 尝试不同的解密策略
                var strategies = new[]
                {
                    () => TryDecryptWithPadding(data, key, PaddingMode.PKCS7),
                    () => TryDecryptWithPadding(data, key, PaddingMode.Zeros),
                    () => TryDecryptWithPadding(data, key, PaddingMode.None),
                    () => TryLegacyDecryption(data, key),
                    () => TryRawDecryption(data, key)
                };

                foreach (var strategy in strategies)
                {
                    try
                    {
                        var result = strategy();
                        if (!string.IsNullOrEmpty(result))
                        {
                            System.Diagnostics.Debug.WriteLine($"使用替代策略成功解密");
                            return result;
                        }
                    }
                    catch
                    {
                        // 继续尝试下一个策略
                    }
                }
                
                throw new CryptographicException("所有解密策略都失败了");
            }
            finally
            {
                if (key != null) Array.Clear(key, 0, key.Length);
                if (data != null) Array.Clear(data, 0, data.Length);
            }
        }

        /// <summary>
        /// 使用指定填充模式尝试解密
        /// </summary>
        private static string TryDecryptWithPadding(byte[] data, byte[] key, PaddingMode padding)
        {
            if (data.Length < 16) return string.Empty;
            
            using var aes = Aes.Create();
            aes.Key = key;
            aes.Mode = CipherMode.CBC;
            aes.Padding = padding;
            
            using var ms = new MemoryStream(data);
            var iv = new byte[16];
            ms.Read(iv, 0, 16);
            aes.IV = iv;
            
            var remainingData = new byte[ms.Length - ms.Position];
            ms.Read(remainingData, 0, remainingData.Length);
            
            if (remainingData.Length == 0) return string.Empty;
            
            using var cs = new CryptoStream(new MemoryStream(remainingData), aes.CreateDecryptor(), CryptoStreamMode.Read);
            using var sr = new StreamReader(cs, Encoding.UTF8);
            
            var result = sr.ReadToEnd();
            
            // 验证结果是否合理
            if (IsValidDecryptionResult(result))
                return result;
            
            return string.Empty;
        }

        /// <summary>
        /// 尝试旧版本兼容的解密方法
        /// </summary>
        private static string TryLegacyDecryption(byte[] data, byte[] key)
        {
            // 实现向后兼容的解密逻辑
            // 这里可以添加对旧版本加密格式的支持
            return string.Empty;
        }

        /// <summary>
        /// 尝试原始数据解密（无IV）
        /// </summary>
        private static string TryRawDecryption(byte[] data, byte[] key)
        {
            if (data.Length < 16) return string.Empty;
            
            try
            {
                using var aes = Aes.Create();
                aes.Key = key;
                aes.Mode = CipherMode.ECB; // 尝试ECB模式
                aes.Padding = PaddingMode.PKCS7;
                
                using var cs = new CryptoStream(new MemoryStream(data), aes.CreateDecryptor(), CryptoStreamMode.Read);
                using var sr = new StreamReader(cs, Encoding.UTF8);
                
                var result = sr.ReadToEnd();
                return IsValidDecryptionResult(result) ? result : string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        /// <summary>
        /// 验证解密结果是否合理
        /// </summary>
        private static bool IsValidDecryptionResult(string result)
        {
            if (string.IsNullOrEmpty(result)) return false;
            
            // 检查是否包含合理的字符
            var validChars = 0;
            var totalChars = result.Length;
            
            foreach (char c in result)
            {
                if (char.IsLetterOrDigit(c) || char.IsPunctuation(c) || char.IsWhiteSpace(c) || c > 127)
                {
                    validChars++;
                }
            }
            
            // 如果超过80%的字符是合理的，认为解密成功
            return (double)validChars / totalChars > 0.8;
        }
        public static string DiagnoseEncryptedData(string cipherBase64)
        {
            if (string.IsNullOrEmpty(cipherBase64))
                return "Cipher text is empty";

            try
            {
                var data = Convert.FromBase64String(cipherBase64);
                var result = $"Base64 decode successful\n";
                result += $"Data length: {data.Length} bytes\n";
                
                if (data.Length < 16)
                {
                    result += "Data length insufficient (requires at least 16 bytes for IV)\n";
                    return result;
                }
                
                result += $"IV length: 16 bytes\n";
                result += $"Encrypted content length: {data.Length - 16} bytes\n";
                
                var encryptedContentLength = data.Length - 16;
                if (encryptedContentLength % 16 == 0)
                {
                    result += "Encrypted content length meets AES block size requirement\n";
                }
                else
                {
                    result += $"Encrypted content length is not multiple of 16 (remainder: {encryptedContentLength % 16})\n";
                }
                
                // 显示IV的前4个字节（用于调试）
                result += $"IV prefix: {Convert.ToHexString(data[0..Math.Min(4, data.Length)])}...\n";
                
                return result;
            }
            catch (FormatException)
            {
                return "Invalid Base64 format";
            }
            catch (Exception ex)
            {
                return $"Diagnosis failed: {ex.Message}";
            }
        }
    }
}
