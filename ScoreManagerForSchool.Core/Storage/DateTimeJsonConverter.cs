using System;
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

                    // 尝试解析各种DateTime格式
                    if (DateTime.TryParse(stringValue, out var dateTime))
                    {
                        return dateTime;
                    }
                    
                    // 如果是DateTimeOffset格式，提取DateTime部分
                    if (DateTimeOffset.TryParse(stringValue, out var dateTimeOffset))
                    {
                        return dateTimeOffset.DateTime;
                    }
                }
                else if (reader.TokenType == JsonTokenType.Number)
                {
                    // 处理Unix时间戳
                    var unixTime = reader.GetInt64();
                    return DateTimeOffset.FromUnixTimeSeconds(unixTime).DateTime;
                }

                // 如果无法解析，返回当前时间
                return DateTime.Now;
            }
            catch
            {
                // 解析失败时返回默认值
                return DateTime.Now;
            }
        }

        public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
        {
            // 使用ISO 8601格式写入，确保一致性
            writer.WriteStringValue(value.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"));
        }
    }
}
