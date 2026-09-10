using System.Text.Json.Serialization;

namespace DownloaderApp.Infrastructure.Dtos
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

        [JsonPropertyName("formats")]
        public List<YtDlpFormatDto>? Formats { get; init; }
    }
}
