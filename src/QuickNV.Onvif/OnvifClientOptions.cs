using System.ServiceModel;
using System.Text.Json.Serialization;

namespace QuickNV.Onvif
{
    [JsonSerializable(typeof(OnvifClientOptions))]
    [JsonSourceGenerationOptions(
DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
WriteIndented = true)]
    public partial class OnvifClientOptionsSerializerContext : JsonSerializerContext { }

    public class OnvifClientOptions
    {
        public string Scheme { get; set; } = "http";
        public string Host { get; set; }
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public int Port { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
        [JsonConverter(typeof(JsonStringEnumConverter<HttpClientCredentialType>))]
        public HttpClientCredentialType ClientCredentialType { get; set; } = HttpClientCredentialType.Digest;
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public int RtspPort { get; set; }
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public int SnapshotPort { get; set; }
    }
}
