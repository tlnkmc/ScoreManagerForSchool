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
                
                using var ms = new MemoryStream(data);
                var iv = new byte[16];
                var ivBytesRead = ms.Read(iv, 0, iv.Length);
                
                if (ivBytesRead != 16)
                    throw new ArgumentException("加密数据格式无效：无法读取IV");
                
                aes.IV = iv;
                
                // 检查是否还有加密数据
                var remainingDataLength = ms.Length - ms.Position;
                if (remainingDataLength <= 0)
                    throw new ArgumentException("加密数据格式无效：没有加密内容");
                
                if (remainingDataLength % 16 != 0)
                    throw new ArgumentException($"加密数据格式无效：加密内容长度 {remainingDataLength} 不是16的倍数");
                
                using var cs = new CryptoStream(ms, aes.CreateDecryptor(), CryptoStreamMode.Read);
                using var sr = new StreamReader(cs, Encoding.UTF8);
                
                try
                {
                    var result = sr.ReadToEnd();
                    
                    // 验证解密结果不为空
                    if (string.IsNullOrEmpty(result))
                        throw new CryptographicException("解密结果为空，可能是密钥错误");
                    
                    return result;
                }
                catch (CryptographicException ex) when (ex.Message.Contains("Padding is invalid"))
                {
                    throw new CryptographicException(
                        "解密失败：填充无效。可能原因：\n" +
                        "1. 密钥不正确\n" +
                        "2. 加密数据已损坏\n" +
                        "3. 数据格式不匹配\n" +
                        $"数据长度: {data.Length} 字节", ex);
                }
                catch (CryptographicException ex)
                {
                    throw new CryptographicException($"解密失败：{ex.Message}。数据长度: {data.Length} 字节", ex);
                }
                catch (Exception ex)
                {
                    throw new CryptographicException($"解密过程中发生未知错误：{ex.Message}", ex);
                }
            }
            catch (FormatException ex)
            {
                throw new ArgumentException("Base64格式无效", nameof(cipherBase64), ex);
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
                throw new ArgumentException("密文不能为空", nameof(cipherBase64));
            
            if (keyBytes == null || keyBytes.Length == 0)
                throw new ArgumentException("密钥不能为空", nameof(keyBytes));

            // 首先尝试正常解密
            try
            {
                return DecryptFromBase64(cipherBase64, keyBytes);
            }
            catch (CryptographicException ex) when (allowFallback)
            {
                // 记录详细错误信息
                var diagnosis = DiagnoseEncryptedData(cipherBase64);
                System.Diagnostics.Debug.WriteLine($"解密失败，诊断信息：\n{diagnosis}");
                System.Diagnostics.Debug.WriteLine($"错误详情：{ex.Message}");
                
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
                return "❌ 密文为空";

            try
            {
                var data = Convert.FromBase64String(cipherBase64);
                var result = $"✅ Base64解码成功\n";
                result += $"📊 数据长度: {data.Length} 字节\n";
                
                if (data.Length < 16)
                {
                    result += "❌ 数据长度不足（需要至少16字节IV）\n";
                    return result;
                }
                
                result += $"📦 IV长度: 16 字节\n";
                result += $"🔐 加密内容长度: {data.Length - 16} 字节\n";
                
                var encryptedContentLength = data.Length - 16;
                if (encryptedContentLength % 16 == 0)
                {
                    result += "✅ 加密内容长度符合AES块大小要求\n";
                }
                else
                {
                    result += $"⚠️ 加密内容长度不是16的倍数（余数: {encryptedContentLength % 16}）\n";
                }
                
                // 显示IV的前4个字节（用于调试）
                result += $"🔑 IV前缀: {Convert.ToHexString(data[0..Math.Min(4, data.Length)])}...\n";
                
                return result;
            }
            catch (FormatException)
            {
                return "❌ Base64格式无效";
            }
            catch (Exception ex)
            {
                return $"❌ 诊断失败: {ex.Message}";
            }
        }
    }
}
