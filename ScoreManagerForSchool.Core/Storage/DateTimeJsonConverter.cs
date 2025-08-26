using System;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ScoreManagerForSchool.Core.Storage
{
    /// <summary>
    /// 自定义DateTime JSON转换器，确保DateTime和DateTimeOffset之间的正确转换
    /// </summary>
    public class DateTimeJsonConverter : JsonConverter<DateTime>
    {
        public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            try
            {
                if (reader.TokenType == JsonTokenType.String)
                {
                    var stringValue = reader.GetString();
                    if (string.IsNullOrEmpty(stringValue))
                        return DateTime.Now;

                    // 兼容旧数据：早期序列化错误地用本地时间却标记了 'Z'（UTC）。
                    // 若尾部是 'Z'，优先尝试去掉 'Z' 按本地时间解析，避免“时间穿越”。
                    var trimmed = stringValue.Trim();
                    if (trimmed.EndsWith("Z", StringComparison.OrdinalIgnoreCase))
                    {
                        var noZ = trimmed[..^1];
                        if (DateTime.TryParse(noZ, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal | DateTimeStyles.AllowWhiteSpaces, out var dtNoZ))
                        {
                            return dtNoZ;
                        }
                    }

                    // 优先解析为 DateTimeOffset，以正确处理包含 Z/偏移的时间
                    if (DateTimeOffset.TryParse(stringValue, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var dto))
                    {
                        return dto.LocalDateTime; // 始终返回本地时间，避免“时间穿越”
                    }

                    // 回退到 DateTime 解析
                    if (DateTime.TryParse(stringValue, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var dt))
                    {
                        // 若为 UTC，转换为本地时间；否则按原样返回
                        return dt.Kind == DateTimeKind.Utc ? dt.ToLocalTime() : dt;
                    }
                }
                else if (reader.TokenType == JsonTokenType.Number)
                {
                    // 处理Unix时间戳（秒），按UTC解释再转本地
                    var unixTime = reader.GetInt64();
                    return DateTimeOffset.FromUnixTimeSeconds(unixTime).LocalDateTime;
                }

                // 如果无法解析，返回当前本地时间
                return DateTime.Now;
            }
            catch
            {
                // 解析失败时返回当前本地时间
                return DateTime.Now;
            }
        }

        public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
        {
            // 以本地时间写入，并包含偏移量，避免误标为Z导致的偏移错误
            var local = value.Kind == DateTimeKind.Utc ? value.ToLocalTime() : value;
            writer.WriteStringValue(local.ToString("o")); // round-trip，包含偏移（K）
        }
    }
}
