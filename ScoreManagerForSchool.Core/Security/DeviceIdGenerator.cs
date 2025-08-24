using System;
using System.Security.Cryptography;
using System.Text;

namespace ScoreManagerForSchool.Core.Security
{
    public static class DeviceIdGenerator
    {
        // 生成256位（64字节）的十六进制字符串（128字节文本表示）
        public static string GenerateDeviceId()
        {
            byte[] bytes = new byte[32]; // 256 bits
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(bytes);
            }
            var sb = new StringBuilder(bytes.Length * 2);
            foreach (var b in bytes)
                sb.Append(b.ToString("X2"));
            return sb.ToString();
        }
    }
}
