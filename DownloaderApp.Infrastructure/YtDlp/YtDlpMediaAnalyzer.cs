using DownloaderApp.Application.Abstractions;
using DownloaderApp.Domain.Models;
using System.Diagnostics;
using System.Text.Json;

namespace DownloaderApp.Infrastructure.YtDlp
{
    public sealed class YtDlpMediaAnalyzer : IMediaAnalyzer
    {
        private readonly string _ytDlpPath;

        public YtDlpMediaAnalyzer(string? ytDlpPath = null)
        {
            if (!string.IsNullOrWhiteSpace(ytDlpPath))
            {
                _ytDlpPath = ytDlpPath;
                return;
            }

            string localPath = Path.Combine(
                AppContext.BaseDirectory,
                "Tools",
                "yt-dlp.exe");

            if (!File.Exists(localPath))
            {
                throw new FileNotFoundException(
                    $"No se encontró yt-dlp en: {localPath}");
            }

            _ytDlpPath = localPath;
        }

        public async Task<MediaInfo> AnalyzeAsync(
            string url,
            CancellationToken cancellationToken = default)
        {
            if (!Uri.TryCreate(url, UriKind.Absolute, out Uri? uri) ||
                (uri.Scheme != Uri.UriSchemeHttp &&
                 uri.Scheme != Uri.UriSchemeHttps))
            {
                throw new ArgumentException("La URL no es válida.");
            }

            var startInfo = new ProcessStartInfo
            {
                FileName = _ytDlpPath,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            startInfo.ArgumentList.Add("--dump-single-json");
            startInfo.ArgumentList.Add("--no-playlist");
            startInfo.ArgumentList.Add("--skip-download");
            startInfo.ArgumentList.Add(url);

            using var process = new Process
            {
                StartInfo = startInfo
            };

            if (!process.Start())
            {
                throw new InvalidOperationException(
                    "No se pudo iniciar yt-dlp.");
            }

            try
            {
                Task<string> outputTask =
                    process.StandardOutput.ReadToEndAsync();

                Task<string> errorTask =
                    process.StandardError.ReadToEndAsync();

                await process.WaitForExitAsync(cancellationToken);

                string output = await outputTask;
                string error = await errorTask;

                if (process.ExitCode != 0)
                {
                    throw new InvalidOperationException(
                        string.IsNullOrWhiteSpace(error)
                            ? "yt-dlp terminó con un error."
                            : error.Trim());
                }

                YtDlpInfoDto? data =
                    JsonSerializer.Deserialize<YtDlpInfoDto>(output);

                if (data is null)
                {
                    throw new InvalidOperationException(
                        "No se pudo interpretar la respuesta de yt-dlp.");
                }

                return new MediaInfo
                {
                    Id = data.Id ?? string.Empty,
                    Title = data.Title ?? "Sin título",
                    Uploader = data.Uploader,
                    ThumbnailUrl = data.Thumbnail,
                    Duration = data.Duration,
                    WebpageUrl = data.WebpageUrl
                };
            }
            catch (OperationCanceledException)
            {
                if (!process.HasExited)
                {
                    process.Kill(entireProcessTree: true);
                }

                throw;
            }
        }


    }
}
