using System;
using System.Collections.Generic;
using System.Text;

namespace DownloaderApp.Domain.Models
{
    public sealed class MediaInfo
    {
        public string Id { get; set; } = string.Empty;

        public string Title { get; set; } = string.Empty;

        public string? ThumbnailUrl { get; set; }

        public string? Uploader { get; set; }

        public double? Duration { get; set; }

        public string? WebpageUrl { get; set; }

        public IReadOnlyList<MediaFormat> Formats { get; init; } = [];
    }
}
