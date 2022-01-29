using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace HEC_SupplyBot
{
    public class ProtocolMetric
    {
        [JsonPropertyName("totalSupply")]
        public string TotalSupply { get; set; }
    }

    public class Data
    {
        [JsonPropertyName("protocolMetrics")]
        public List<ProtocolMetric> ProtocolMetrics { get; set; }
    }

    public class HecApiResponse
    {
        [JsonPropertyName("data")]
        public Data Data { get; set; }
    }
}