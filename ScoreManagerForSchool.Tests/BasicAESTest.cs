using System;
using System.Security.Cryptography;
using System.Text;
using System.IO;

namespace ScoreManagerForSchool.Tests
{
    public static class BasicAESTest
    {
        public static void TestBasicAES()
        {
            Console.WriteLine("=== Basic AES Test ===");
            
            var plaintext = "Hello World 123";
            var key = new byte[32];
            for (int i = 0; i < 32; i++)
            {
                key[i] = (byte)(i % 256);
            }
            
            try
            {
                // 直接使用.NET的AES实现
                using var aes = Aes.Create();
                aes.Key = key;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;
                aes.GenerateIV();
                
                Console.WriteLine($"AES Key length: {aes.Key.Length}");
                Console.WriteLine($"AES IV length: {aes.IV.Length}");
                Console.WriteLine($"AES Mode: {aes.Mode}");
                Console.WriteLine($"AES Padding: {aes.Padding}");
                
                // 加密（使用 TransformFinalBlock 确保一致性）
                byte[] encrypted;
                using (var encryptor = aes.CreateEncryptor(aes.Key, aes.IV))
                {
                    var plainBytes = Encoding.UTF8.GetBytes(plaintext);
                    var cipher = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);
                    encrypted = new byte[aes.IV.Length + cipher.Length];
                    Buffer.BlockCopy(aes.IV, 0, encrypted, 0, aes.IV.Length);
                    Buffer.BlockCopy(cipher, 0, encrypted, aes.IV.Length, cipher.Length);
                }
                
                Console.WriteLine($"Encrypted length: {encrypted.Length} bytes");
                Console.WriteLine($"Expected length: {16 + ((plaintext.Length / 16 + 1) * 16)} bytes (IV + padded data)");
                
                // 解密
                using var aesDecrypt = Aes.Create();
                aesDecrypt.Key = key;
                aesDecrypt.Mode = CipherMode.CBC;
                aesDecrypt.Padding = PaddingMode.PKCS7;
                
                using var msDecrypt = new MemoryStream(encrypted);
                
                // 读取IV
                var iv = new byte[16];
                var bytesRead = msDecrypt.Read(iv, 0, 16);
                aesDecrypt.IV = iv;
                
                Console.WriteLine($"Read IV length: {bytesRead}");
                Console.WriteLine($"Remaining data length: {msDecrypt.Length - msDecrypt.Position}");
                
                using var decryptor = aesDecrypt.CreateDecryptor(aesDecrypt.Key, aesDecrypt.IV);
                // 直接使用 TransformFinalBlock 解密剩余密文
                var cipherLen = (int)(msDecrypt.Length - msDecrypt.Position);
                var cipherBytes = new byte[cipherLen];
                var actuallyRead = msDecrypt.Read(cipherBytes, 0, cipherLen);
                Console.WriteLine($"Cipher bytes to read: {cipherLen}, actually read: {actuallyRead}");
                if (actuallyRead != cipherLen)
                {
                    Array.Resize(ref cipherBytes, actuallyRead);
                }
                Console.WriteLine($"Cipher hex: {BitConverter.ToString(cipherBytes).Replace("-", string.Empty)}");
                var decryptedBytes = decryptor.TransformFinalBlock(cipherBytes, 0, cipherBytes.Length);
                var decrypted = Encoding.UTF8.GetString(decryptedBytes);
                Console.WriteLine($"Decrypted bytes read: {decryptedBytes.Length}");
                
                Console.WriteLine($"Original:  '{plaintext}'");
                Console.WriteLine($"Decrypted: '{decrypted}'");
                
                if (plaintext == decrypted)
                {
                    Console.WriteLine("✅ Basic AES test PASSED");
                }
                else
                {
                    Console.WriteLine("❌ Basic AES test FAILED - text mismatch");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Basic AES test FAILED with exception: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                throw;
            }
        }
    }
}
