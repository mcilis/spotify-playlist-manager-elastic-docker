using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Domain
{
    public class Artist
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("uri")]
        public string Uri { get; set; }
    }
}