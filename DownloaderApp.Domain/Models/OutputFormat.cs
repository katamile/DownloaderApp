using System;
using System.Collections.Generic;
using System.Text;

namespace DownloaderApp.Domain.Models
{
    public sealed class OutputFormat
    {
        public required string Name { get; init; }

        public required string Extension { get; init; }

        public required bool IsAudioOnly { get; init; }

        public override string ToString()
        {
            return Name;
        }
    }
}
