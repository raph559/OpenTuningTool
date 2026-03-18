using System.Text.Json.Serialization;

namespace OpenTuningTool.Services;

/// <summary>
/// JSON response model matching CalibrAI's MapCandidateResponse (snake_case).
/// </summary>
public class MapCandidateResult
{
    [JsonPropertyName("address")]           public int Address { get; set; }
    [JsonPropertyName("address_hex")]       public string AddressHex { get; set; } = "";
    [JsonPropertyName("byte_size")]         public int ByteSize { get; set; }
    [JsonPropertyName("rows")]              public int Rows { get; set; }
    [JsonPropertyName("cols")]              public int Cols { get; set; }
    [JsonPropertyName("element_size_bits")] public int ElementSizeBits { get; set; }
    [JsonPropertyName("endian")]            public string Endian { get; set; } = "little";
    [JsonPropertyName("confidence")]        public float Confidence { get; set; }
    [JsonPropertyName("map_class_prob")]    public float MapClassProb { get; set; }
    [JsonPropertyName("x_axis_address")]    public int? XAxisAddress { get; set; }
    [JsonPropertyName("x_axis_length")]     public int? XAxisLength { get; set; }
    [JsonPropertyName("y_axis_address")]    public int? YAxisAddress { get; set; }
    [JsonPropertyName("y_axis_length")]     public int? YAxisLength { get; set; }
}
