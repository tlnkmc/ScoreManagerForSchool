using System;
using System.Security.Cryptography;
using System.Text;
using ScoreManagerForSchool.Core.Security;
using ScoreManagerForSchool.Core.Storage;

namespace ScoreManagerForSchool.Tests
{
    /// <summary>
    /// 加密系统测试类
    /// </summary>
    public static class CryptoTestHelper
    {
        /// <summary>
        /// 测试基础加密解密功能
        /// </summary>
        public static void TestBasicCrypto()
        {
            Console.WriteLine("🔐 测试基础加密解密功能...");
            
            try
            {
                var testData = "这是一个测试字符串，包含中文和English文本！@#$%^&*()";
                var key = new byte[32];
                Random.Shared.NextBytes(key);
                
                // 测试加密
                var encrypted = CryptoUtil.EncryptToBase64(testData, key);
                Console.WriteLine($"✅ 加密成功，密文长度: {encrypted.Length}");
                
                // 诊断加密数据
                var diagnosis = CryptoUtil.DiagnoseEncryptedData(encrypted);
                Console.WriteLine($"📊 加密数据诊断:\n{diagnosis}");
                
                // 测试解密
                var decrypted = CryptoUtil.DecryptFromBase64(encrypted, key);
                Console.WriteLine($"✅ 解密成功");
                
                // 验证数据一致性
                if (testData == decrypted)
                {
                    Console.WriteLine("✅ 数据一致性验证通过");
                }
                else
                {
                    Console.WriteLine("❌ 数据一致性验证失败");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 基础加密测试失败: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 测试环境密钥系统
        /// </summary>
        public static void TestEnvironmentKey()
        {
            Console.WriteLine("\n🖥️ 测试环境密钥系统...");
            
            try
            {
                var envDiagnostics = KeyManager.GetEnvironmentDiagnostics();
                Console.WriteLine(envDiagnostics);
                
                Console.WriteLine("✅ 环境密钥系统正常");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 环境密钥测试失败: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 测试错误场景
        /// </summary>
        public static void TestErrorScenarios()
        {
            Console.WriteLine("\n⚠️ 测试错误场景处理...");
            
            // 测试空数据
            try
            {
                CryptoUtil.DecryptFromBase64("", new byte[32]);
                Console.WriteLine("❌ 空数据测试失败：应该抛出异常");
            }
            catch (ArgumentException)
            {
                Console.WriteLine("✅ 空数据异常处理正确");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ 空数据异常类型不匹配: {ex.GetType().Name}");
            }
            
            // 测试无效Base64
            try
            {
                CryptoUtil.DecryptFromBase64("invalid_base64!", new byte[32]);
                Console.WriteLine("❌ 无效Base64测试失败：应该抛出异常");
            }
            catch (ArgumentException)
            {
                Console.WriteLine("✅ 无效Base64异常处理正确");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ 无效Base64异常类型不匹配: {ex.GetType().Name}");
            }
            
            // 测试错误密钥
            try
            {
                var validEncrypted = CryptoUtil.EncryptToBase64("test", new byte[32]);
                var wrongKey = new byte[32];
                Random.Shared.NextBytes(wrongKey);
                
                CryptoUtil.DecryptFromBase64(validEncrypted, wrongKey);
                Console.WriteLine("❌ 错误密钥测试失败：应该抛出异常");
            }
            catch (CryptographicException)
            {
                Console.WriteLine("✅ 错误密钥异常处理正确");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ 错误密钥异常类型不匹配: {ex.GetType().Name}: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 运行所有测试
        /// </summary>
        public static void RunAllTests()
        {
            Console.WriteLine("🚀 开始加密系统测试\n");
            
            TestBasicCrypto();
            TestEnvironmentKey();
            TestErrorScenarios();
            
            Console.WriteLine("\n🎉 加密系统测试完成");
        }
    }
}
