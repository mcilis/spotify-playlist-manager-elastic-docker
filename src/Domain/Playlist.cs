using System;
using System.Text.Json.Serialization;

namespace Domain
{
    public class Playlist
    {
        public int PlaylistId { get; set; }

        [JsonPropertyName("id")]
        public string Code { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("public")]
        public bool Public { get; set; }

        public string Uri { get; set; }

        public int TracksCount { get; set; }

        public DateTime CreateDate { get; set; }
    }
}