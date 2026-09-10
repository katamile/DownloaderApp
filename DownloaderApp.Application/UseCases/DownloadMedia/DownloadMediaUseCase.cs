using DownloaderApp.Application.Abstractions;
using DownloaderApp.Domain.Models;

namespace DownloaderApp.Application.UseCases.DownloadMedia;

public sealed class DownloadMediaUseCase
{
    private readonly IMediaDownloader _mediaDownloader;

    public DownloadMediaUseCase(
        IMediaDownloader mediaDownloader)
    {
        _mediaDownloader = mediaDownloader;
    }

    public async Task ExecuteAsync(
        string url,
        MediaFormat format,
        OutputFormat outputFormat,
        string outputDirectory,
        IProgress<DownloadProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            throw new ArgumentException(
                "La URL no puede estar vacía.",
                nameof(url));
        }

        ArgumentNullException.ThrowIfNull(format);
        ArgumentNullException.ThrowIfNull(outputFormat);

        if (string.IsNullOrWhiteSpace(outputDirectory))
        {
            throw new ArgumentException(
                "El directorio de salida no puede estar vacío.",
                nameof(outputDirectory));
        }

        await _mediaDownloader.DownloadAsync(
            url,
            format,
            outputFormat,
            outputDirectory,
            progress,
            cancellationToken);
    }
}