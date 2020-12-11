using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Domain
{
    public class Track
    {
        public int TrackId { get; set; }

        [JsonPropertyName("id")]
        public string Code { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("popularity")]
        public int Popularity { get; set; }

        [JsonPropertyName("uri")]
        public string Uri { get; set; }

        [JsonPropertyName("artists")]
        public List<Artist> Artists { get; set; }
        
        public string Artist { get; set; }

        public string SearchText { get; set; }

        public DateTime CreateDate { get; set; }
    }
}
