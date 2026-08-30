using DownloaderApp.Domain.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace DownloaderApp.Application.Abstractions
{
    public interface IMediaAnalyzer
    {
        Task<MediaInfo> AnalyzeAsync(
            string url,
            CancellationToken cancellationToken = default);
    }
}
