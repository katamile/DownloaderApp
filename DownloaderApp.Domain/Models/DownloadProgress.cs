using System;
using System.Collections.Generic;
using System.Text;

namespace DownloaderApp.Domain.Models
{
    public sealed class DownloadProgress
    {
        public double Percentage { get; init; }

        public string? Speed { get; init; }

        public string? Eta { get; init; }

        public string? Status { get; init; }
    }
}
