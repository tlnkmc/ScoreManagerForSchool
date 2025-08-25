using Xunit;
using ScoreManagerForSchool.Tests;
using System;

namespace ScoreManagerForSchool.Tests
{
    public class CryptographicExceptionFixTests
    {
        [Fact]
        public void TestBasicEncryptionDecryption()
        {
            // 这个测试验证基础加密解密功能是否正常
            AssertExtensions.DoesNotThrow(() =>
            {
                CryptoTestHelper.TestBasicCrypto();
            });
        }

        [Fact]
        public void TestEnvironmentKeyGeneration()
        {
            // 这个测试验证环境密钥生成是否正常
            AssertExtensions.DoesNotThrow(() =>
            {
                CryptoTestHelper.TestEnvironmentKey();
            });
        }

        [Fact]
        public void TestErrorHandling()
        {
            // 这个测试验证错误处理是否正确
            AssertExtensions.DoesNotThrow(() =>
            {
                CryptoTestHelper.TestErrorScenarios();
            });
        }

        [Fact]
        public void TestComprehensiveCryptoSystem()
        {
            // 综合测试所有加密系统功能
            AssertExtensions.DoesNotThrow(() =>
            {
                CryptoTestHelper.RunAllTests();
            });
        }
    }

    // 扩展Assert类以支持DoesNotThrow
    public static class AssertExtensions
    {
        public static void DoesNotThrow(Action action)
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                throw new Xunit.Sdk.XunitException($"Expected no exception, but got: {ex.GetType().Name}: {ex.Message}");
            }
        }
    }
}
