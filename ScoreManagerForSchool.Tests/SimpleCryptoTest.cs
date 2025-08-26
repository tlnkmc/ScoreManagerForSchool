using System;
using System.Text;
using System.Security.Cryptography;
using ScoreManagerForSchool.Core.Security;

namespace ScoreManagerForSchool.Tests
{
    public static class SimpleCryptoTest
    {
        public static void RunSimpleTest()
        {
            Console.WriteLine("=== Simple Crypto Test ===");
            
            // 首先运行基础AES测试
            BasicAESTest.TestBasicAES();
            
            Console.WriteLine("\n=== CryptoUtil Test ===");
            
            // 使用固定的测试数据
            var testData = "Hello World 123";
            var fixedKey = new byte[32];
            for (int i = 0; i < 32; i++)
            {
                fixedKey[i] = (byte)(i % 256);
            }
            
            try
            {
                Console.WriteLine($"Original text: {testData}");
                Console.WriteLine($"Key length: {fixedKey.Length}");
                
                // 加密
                var encrypted = CryptoUtil.EncryptToBase64(testData, fixedKey);
                Console.WriteLine($"Encrypted (Base64): {encrypted}");
                Console.WriteLine($"Encrypted length: {encrypted.Length}");
                
                // 解密（路径1：使用库方法）
                var decrypted = CryptoUtil.DecryptFromBase64(encrypted, fixedKey);
                Console.WriteLine($"Decrypted text: {decrypted}");

                // 解密（路径2：手动拆分+TransformFinalBlock 验证）
                var raw = Convert.FromBase64String(encrypted);
                Console.WriteLine($"Raw length: {raw.Length}, IV len: 16, cipher len: {raw.Length - 16}");
                using (var aes = Aes.Create())
                {
                    aes.Key = fixedKey;
                    aes.Mode = CipherMode.CBC;
                    aes.Padding = PaddingMode.PKCS7;
                    var iv = new byte[16];
                    Buffer.BlockCopy(raw, 0, iv, 0, 16);
                    var cipher = new byte[raw.Length - 16];
                    Buffer.BlockCopy(raw, 16, cipher, 0, cipher.Length);
                    using var dec = aes.CreateDecryptor(aes.Key, iv);
                    var plain = dec.TransformFinalBlock(cipher, 0, cipher.Length);
                    var manual = Encoding.UTF8.GetString(plain);
                    Console.WriteLine($"Manual decrypted: {manual}");
                }
                
                // 验证
                if (testData == decrypted)
                {
                    Console.WriteLine("✅ Simple crypto test PASSED");
                }
                else
                {
                    Console.WriteLine("❌ Simple crypto test FAILED - text mismatch");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Simple crypto test FAILED with exception: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                throw;
            }
        }
    }
}
