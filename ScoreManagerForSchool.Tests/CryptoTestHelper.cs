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
            Console.WriteLine("Testing basic encryption and decryption...");
            SimpleCryptoTest.RunSimpleTest();
        }
        
        /// <summary>
        /// 测试环境密钥系统
        /// </summary>
        public static void TestEnvironmentKey()
        {
            Console.WriteLine("Testing environment key system...");
            
            var envDiagnostics = KeyManager.GetEnvironmentDiagnostics();
            Console.WriteLine(envDiagnostics);
            
            Console.WriteLine("Environment key system normal");
        }
        
        /// <summary>
        /// 测试错误场景
        /// </summary>
        public static void TestErrorScenarios()
        {
            Console.WriteLine("Testing error scenario handling...");
            
            // 测试空数据
            try
            {
                CryptoUtil.DecryptFromBase64("", new byte[32]);
                Console.WriteLine("Empty data test failed: should throw exception");
            }
            catch (ArgumentException)
            {
                Console.WriteLine("Empty data exception handling correct");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Empty data exception type mismatch: {ex.GetType().Name}");
            }
            
            // 测试无效Base64
            try
            {
                CryptoUtil.DecryptFromBase64("invalid_base64!", new byte[32]);
                Console.WriteLine("Invalid Base64 test failed: should throw exception");
            }
            catch (ArgumentException)
            {
                Console.WriteLine("Invalid Base64 exception handling correct");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Invalid Base64 exception type mismatch: {ex.GetType().Name}");
            }
            
            // 测试错误密钥
            try
            {
                var validEncrypted = CryptoUtil.EncryptToBase64("test", new byte[32]);
                var wrongKey = new byte[32];
                Random.Shared.NextBytes(wrongKey);
                
                CryptoUtil.DecryptFromBase64(validEncrypted, wrongKey);
                Console.WriteLine("Wrong key test failed: should throw exception");
            }
            catch (CryptographicException)
            {
                Console.WriteLine("Wrong key exception handling correct");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Wrong key exception type mismatch: {ex.GetType().Name}: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 运行所有测试
        /// </summary>
        public static void RunAllTests()
        {
            Console.WriteLine("Starting encryption system tests");
            
            TestBasicCrypto();
            TestEnvironmentKey();
            TestErrorScenarios();
            
            Console.WriteLine("Encryption system tests completed");
        }
    }
}
