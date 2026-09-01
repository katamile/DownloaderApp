using System;
using System.Collections.Generic;
using System.Text;

namespace DownloaderApp.Domain.Models
{
    public sealed class MediaFormat
    {
        public string FormatId { get; init; } = string.Empty;

        public string? Extension { get; init; }

        public int? Width { get; init; }

        public int? Height { get; init; }

        public double? Fps { get; init; }

        public string? VideoCodec { get; init; }

        public string? AudioCodec { get; init; }

        public double? FileSize { get; init; }

        public double? ApproximateFileSize { get; init; }

        public double? AudioBitrate { get; init; }

        public bool HasVideo =>
            !string.IsNullOrWhiteSpace(VideoCodec) &&
            VideoCodec != "none";

        public bool HasAudio =>
            !string.IsNullOrWhiteSpace(AudioCodec) &&
            AudioCodec != "none";

        public string DisplayName
        {
            get
            {
                if (HasVideo)
                {
                    var resolution = Height is not null
                        ? $"{Height}p"
                        : "Video";

                    var fps = Fps is > 30
                        ? $" • {Fps:0} FPS"
                        : string.Empty;

                    var audio = HasAudio
                        ? " • Video + Audio"
                        : " • Solo video";

                    return $"{resolution}{fps} • {Extension?.ToUpper()}{audio}";
                }

                if (HasAudio)
                {
                    var bitrate = AudioBitrate is not null
                        ? $" • {AudioBitrate:0} kbps"
                        : string.Empty;

                    return $"Audio • {Extension?.ToUpper()}{bitrate}";
                }

                return $"{FormatId} • {Extension?.ToUpper()}";
            }
        }
    }
}
