using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using ScoreManagerForSchool.Core.Security;

namespace ScoreManagerForSchool.Core.Storage
{
    /// <summary>
    /// 通用加密数据库存储类，用于替代JSON文件存储
    /// </summary>
    /// <typeparam name="T">要存储的数据类型</typeparam>
    public class EncryptedDataStore<T>
    {
        private readonly string _path;
        private readonly string _baseDir;

        public EncryptedDataStore(string baseDir, string fileName)
        {
            _baseDir = baseDir;
            Directory.CreateDirectory(baseDir);
            _path = Path.Combine(baseDir, fileName + ".edb"); // encrypted database
        }

        /// <summary>
        /// 加载数据
        /// </summary>
        public T? Load()
        {
            if (!File.Exists(_path)) return default(T);
            
            try
            {
                // 读取加密文件
                var encryptedData = File.ReadAllText(_path);
                if (string.IsNullOrWhiteSpace(encryptedData)) return default(T);

                // 获取加密密钥
                var key = GetEncryptionKey();
                if (!KeyManager.ValidateKey(key)) return default(T);

                try
                {
                    // 使用安全解密方法
                    var decryptedJson = CryptoUtil.SafeDecryptFromBase64(encryptedData, key!, true);
                    
                    // 反序列化JSON，使用一致的日期时间格式
                    var options = new JsonSerializerOptions
                    {
                        WriteIndented = true
                    };
                    return JsonSerializer.Deserialize<T>(decryptedJson, options);
                }
                finally
                {
                    // 清除密钥
                    if (key != null) Array.Clear(key, 0, key.Length);
                }
            }
            catch
            {
                // 如果解密失败，返回默认值
                return default(T);
            }
        }

        /// <summary>
        /// 保存数据
        /// </summary>
        public void Save(T data)
        {
            try
            {
                // 序列化为JSON
                var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });

                // 获取加密密钥
                var key = GetEncryptionKey();
                if (!KeyManager.ValidateKey(key)) 
                    throw new InvalidOperationException("无法获取有效的加密密钥");

                try
                {
                    // 加密数据
                    var encryptedData = CryptoUtil.EncryptToBase64(json, key!);
                    
                    // 写入文件
                    File.WriteAllText(_path, encryptedData);
                }
                finally
                {
                    // 清除密钥
                    if (key != null) Array.Clear(key, 0, key.Length);
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"保存加密数据失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 检查数据文件是否存在
        /// </summary>
        public bool Exists()
        {
            return File.Exists(_path);
        }

        /// <summary>
        /// 删除数据文件
        /// </summary>
        public void Delete()
        {
            if (File.Exists(_path))
            {
                File.Delete(_path);
            }
        }

        /// <summary>
        /// 获取加密密钥
        /// 通过KeyManager安全地获取密钥，避免在代码中硬编码
        /// </summary>
        private byte[]? GetEncryptionKey()
        {
            return KeyManager.GetDataEncryptionKey(_baseDir);
        }
    }
}
