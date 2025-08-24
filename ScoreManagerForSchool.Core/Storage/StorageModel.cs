using System;
using System.Text.Json.Serialization;

namespace ScoreManagerForSchool.Core.Storage
{
    public class Database1Model
    {
        [JsonPropertyName("ID1")]
    public string? ID1 { get; set; }


    [JsonPropertyName("Salt1")]
    public string? Salt1 { get; set; }

    // ID2/Salt2 removed since second password is deprecated

    [JsonPropertyName("Iterations")]
    public int Iterations { get; set; } = 100000;
    }
}
