using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace DownloaderApp.Domain.Models
{
    public sealed class YtDlpInfoDto
    {
        [JsonPropertyName("id")]
        public string? Id { get; init; }

        [JsonPropertyName("title")]
        public string? Title { get; init; }

        [JsonPropertyName("uploader")]
        public string? Uploader { get; init; }

        [JsonPropertyName("thumbnail")]
        public string? Thumbnail { get; init; }

        [JsonPropertyName("duration")]
        public double? Duration { get; init; }

        [JsonPropertyName("webpage_url")]
        public string? WebpageUrl { get; init; }
    }
}
