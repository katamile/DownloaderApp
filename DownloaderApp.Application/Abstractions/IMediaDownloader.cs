using DownloaderApp.Domain.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace DownloaderApp.Application.Abstractions
{
    public interface IMediaDownloader
    {
        Task DownloadAsync(
            string url,
            MediaFormat format,
            OutputFormat outputFormat,
            string outputDirectory,
            IProgress<DownloadProgress>? progress = null,
            CancellationToken cancellationToken = default);
    }
}
