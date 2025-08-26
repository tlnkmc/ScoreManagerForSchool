using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using ScoreManagerForSchool.Core.Security;

namespace ScoreManagerForSchool.Core.Storage
{
    /// <summary>
    /// 密钥管理器，负责安全地生成和管理加密密钥
    /// 确保密钥的持久性和一致性
    /// </summary>
    public static class KeyManager
    {
        private static readonly string KeyDerivationSalt = "SMFS_KDF_SALT_2024";
        private static readonly int KeyIterations = 100000;
        private static readonly string MasterKeyFileName = ".master_key";
        
        /// <summary>
        /// 获取数据加密密钥
        /// 首次运行时生成并保存密钥，后续运行使用相同密钥
        /// </summary>
        public static byte[]? GetDataEncryptionKey(string baseDir)
        {
            try
            {
                // 首先尝试从文件加载已保存的密钥
                var savedKey = LoadSavedKey(baseDir);
                if (savedKey != null && ValidateKey(savedKey))
                {
                    return savedKey;
                }
                
                // 如果没有保存的密钥，生成新密钥并保存
                var newKey = GenerateAndSaveKey(baseDir);
                return newKey;
            }
            catch
            {
                return null;
            }
        }
        
        /// <summary>
        /// 从文件加载已保存的密钥
        /// </summary>
        private static byte[]? LoadSavedKey(string baseDir)
        {
            try
            {
                var keyFilePath = GetMasterKeyFilePath(baseDir);
                if (!File.Exists(keyFilePath))
                {
                    return null;
                }
                
                // 读取Base64编码的加密密钥数据
                var encryptedBase64 = File.ReadAllText(keyFilePath, Encoding.UTF8);
                
                if (string.IsNullOrWhiteSpace(encryptedBase64))
                {
                    throw new InvalidOperationException("密钥文件为空");
                }
                
                // 使用环境因子作为解密密钥
                var envKey = GenerateEnvironmentKey();
                
                try
                {
                    // 使用安全解密方法，包含错误恢复
                    var decryptedBase64 = CryptoUtil.SafeDecryptFromBase64(encryptedBase64, envKey, true);
                    var masterKey = Convert.FromBase64String(decryptedBase64);
                    
                    if (masterKey.Length != 32)
                    {
                        throw new InvalidOperationException($"主密钥长度错误：期望32字节，实际{masterKey.Length}字节");
                    }
                    
                    return masterKey;
                }
                catch (CryptographicException ex)
                {
                    // 提供详细的诊断信息
                    var diagnosis = CryptoUtil.DiagnoseEncryptedData(encryptedBase64);
                    var envDiagnosis = GetEnvironmentDiagnostics();
                    var errorDetails = $"无法解密主密钥：\n\n📊 加密数据诊断：\n{diagnosis}\n\n{envDiagnosis}\n" +
                        "🔍 可能原因：\n" +
                        "1. 环境变化（机器名、用户名、操作系统）\n" +
                        "2. 密钥文件损坏\n" +
                        "3. 系统时间异常\n" +
                        "4. 用户账户权限变化\n\n" +
                        $"原始错误：{ex.Message}";
                    
                    throw new InvalidOperationException(errorDetails, ex);
                }
            }
            catch (Exception ex)
            {
                // 记录详细错误但不抛出，允许生成新密钥
                System.Diagnostics.Debug.WriteLine($"加载主密钥失败: {ex.Message}");
                
                // 如果是关键的解密错误，可能需要用户介入
                if (ex is CryptographicException || ex.InnerException is CryptographicException)
                {
                    // 可以考虑在这里提示用户环境可能发生了变化
                    System.Diagnostics.Debug.WriteLine("可能的原因：机器名、用户名或操作系统发生了变化");
                }
                
                return null;
            }
        }
        
        /// <summary>
        /// 生成新密钥并保存到文件
        /// </summary>
        private static byte[]? GenerateAndSaveKey(string baseDir)
        {
            try
            {
                // 生成随机的256位密钥
                var key = new byte[32];
                using (var rng = RandomNumberGenerator.Create())
                {
                    rng.GetBytes(key);
                }
                
                // 保存密钥到文件
                if (SaveKeyToFile(key, baseDir))
                {
                    return key;
                }
                
                return null;
            }
            catch
            {
                return null;
            }
        }
        
        /// <summary>
        /// 将密钥保存到加密文件
        /// </summary>
        private static bool SaveKeyToFile(byte[] key, string baseDir)
        {
            try
            {
                var keyFilePath = GetMasterKeyFilePath(baseDir);
                var keyDir = Path.GetDirectoryName(keyFilePath);
                
                if (!string.IsNullOrEmpty(keyDir) && !Directory.Exists(keyDir))
                {
                    Directory.CreateDirectory(keyDir);
                }
                
                // 使用环境因子加密密钥
                var envKey = GenerateEnvironmentKey();
                var keyBase64 = Convert.ToBase64String(key);
                var encryptedBase64 = CryptoUtil.EncryptToBase64(keyBase64, envKey);
                
                // 保存到文件，设置为隐藏文件
                File.WriteAllText(keyFilePath, encryptedBase64, Encoding.UTF8);
                File.SetAttributes(keyFilePath, FileAttributes.Hidden);
                
                return true;
            }
            catch
            {
                return false;
            }
        }
        
        /// <summary>
        /// 生成基于环境的密钥（用于加密主密钥）
        /// </summary>
        private static byte[] GenerateEnvironmentKey()
        {
            // 组合多个环境因子生成种子
            var seedComponents = new[]
            {
                Environment.MachineName ?? "unknown_machine",
                Environment.UserName ?? "unknown_user", 
                Environment.OSVersion.ToString(),
                "SMFS_EnvKey_2024"
            };
            
            // 将所有组件组合成种子字符串
            var seed = string.Join("|", seedComponents);
            
            // 使用固定的盐进行密钥派生
            var salt = Encoding.UTF8.GetBytes(KeyDerivationSalt + "_ENV");
            
            // 派生32字节的加密密钥
            return CryptoUtil.DeriveKey(seed, salt, 32, KeyIterations) ?? new byte[32];
        }

        /// <summary>
        /// 获取当前环境信息的诊断字符串
        /// </summary>
        public static string GetEnvironmentDiagnostics()
        {
            try
            {
                var result = "Environment information diagnostics:\n";
                result += $"Machine name: {Environment.MachineName ?? "Unknown"}\n";
                result += $"User name: {Environment.UserName ?? "Unknown"}\n";
                result += $"Operating system: {Environment.OSVersion}\n";
                result += $"Processor count: {Environment.ProcessorCount}\n";
                result += $"System directory: {Environment.SystemDirectory}\n";
                
                var envKey = GenerateEnvironmentKey();
                result += $"Environment key hash: {Convert.ToHexString(envKey)[0..8]}...\n";
                
                return result;
            }
            catch (Exception ex)
            {
                return $"Failed to get environment information: {ex.Message}";
            }
        }
        
        /// <summary>
        /// 获取主密钥文件路径
        /// </summary>
        private static string GetMasterKeyFilePath(string baseDir)
        {
            return Path.Combine(baseDir, MasterKeyFileName);
        }
        
        /// <summary>
        /// 获取或创建应用实例ID
        /// 每个应用安装都有唯一的实例ID，存储在本地文件中
        /// </summary>
        private static string GetOrCreateAppInstanceId(string baseDir)
        {
            try
            {
                var instanceIdFile = Path.Combine(baseDir, ".app_instance");
                
                if (File.Exists(instanceIdFile))
                {
                    var existingId = File.ReadAllText(instanceIdFile);
                    if (!string.IsNullOrWhiteSpace(existingId))
                    {
                        return existingId.Trim();
                    }
                }
                
                // 生成新的实例ID
                var newInstanceId = GenerateInstanceId();
                
                // 保存到文件
                Directory.CreateDirectory(baseDir);
                File.WriteAllText(instanceIdFile, newInstanceId);
                
                // 设置文件为隐藏
                try
                {
                    File.SetAttributes(instanceIdFile, FileAttributes.Hidden);
                }
                catch { /* 忽略设置隐藏属性的错误 */ }
                
                return newInstanceId;
            }
            catch
            {
                // 如果无法读写文件，返回基于时间的fallback ID
                return $"fallback_{DateTime.Now:yyyyMMddHHmmss}";
            }
        }
        
        /// <summary>
        /// 生成应用实例ID
        /// </summary>
        private static string GenerateInstanceId()
        {
            // 使用时间戳和随机数生成唯一ID
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var randomBytes = new byte[16];
            
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(randomBytes);
            }
            
            var randomHex = Convert.ToHexString(randomBytes);
            return $"{timestamp:X}_{randomHex}";
        }
        
        /// <summary>
        /// 验证密钥是否可用
        /// </summary>
        public static bool ValidateKey(byte[]? key)
        {
            return key != null && key.Length == 32;
        }
    }
}
