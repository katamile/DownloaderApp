using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace DownloaderApp.Domain.Models
{
    public sealed class YtDlpFormatDto
    {
        [JsonPropertyName("format_id")]
        public string? FormatId { get; init; }

        [JsonPropertyName("ext")]
        public string? Extension { get; init; }

        [JsonPropertyName("width")]
        public int? Width { get; init; }

        [JsonPropertyName("height")]
        public int? Height { get; init; }

        [JsonPropertyName("fps")]
        public double? Fps { get; init; }

        [JsonPropertyName("vcodec")]
        public string? VideoCodec { get; init; }

        [JsonPropertyName("acodec")]
        public string? AudioCodec { get; init; }

        [JsonPropertyName("filesize")]
        public double? FileSize { get; init; }

        [JsonPropertyName("filesize_approx")]
        public double? ApproximateFileSize { get; init; }

        [JsonPropertyName("abr")]
        public double? AudioBitrate { get; init; }
    }
}
