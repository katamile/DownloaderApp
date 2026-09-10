using DownloaderApp.Application.Abstractions;
using DownloaderApp.Domain.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace DownloaderApp.Application.UseCases.AnalyzeMedia
{
    public sealed class AnalyzeMediaUseCase
    {
        private readonly IMediaAnalyzer _mediaAnalyzer;

        public AnalyzeMediaUseCase(
            IMediaAnalyzer mediaAnalyzer)
        {
            _mediaAnalyzer = mediaAnalyzer;
        }

        public async Task<MediaInfo> ExecuteAsync(
            string url,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                throw new ArgumentException(
                    "La URL no puede estar vacía.",
                    nameof(url));
            }

            return await _mediaAnalyzer.AnalyzeAsync(
                url,
                cancellationToken);
        }
    }
}
