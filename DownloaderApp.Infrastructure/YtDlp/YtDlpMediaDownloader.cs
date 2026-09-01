using DownloaderApp.Application.Abstractions;
using DownloaderApp.Domain.Models;
using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DownloaderApp.Infrastructure.YtDlp;

public sealed class YtDlpMediaDownloader : IMediaDownloader
{
    private const string ProgressPrefix = "DLPROGRESS|";

    private readonly string _ytDlpPath;
    private readonly string _ffmpegDirectory;

    public YtDlpMediaDownloader(
        string? ytDlpPath = null,
        string? ffmpegDirectory = null)
    {
        string toolsDirectory = Path.Combine(
            AppContext.BaseDirectory,
            "Tools");

        string localYtDlpPath = Path.Combine(
            toolsDirectory,
            "yt-dlp.exe");

        string localFfmpegPath = Path.Combine(
            toolsDirectory,
            "ffmpeg.exe");

        // yt-dlp
        if (!string.IsNullOrWhiteSpace(ytDlpPath))
        {
            _ytDlpPath = ytDlpPath;
        }
        else
        {
            if (!File.Exists(localYtDlpPath))
            {
                throw new FileNotFoundException(
                    $"No se encontró yt-dlp en: {localYtDlpPath}");
            }

            _ytDlpPath = localYtDlpPath;
        }

        // FFmpeg
        if (!string.IsNullOrWhiteSpace(ffmpegDirectory))
        {
            _ffmpegDirectory = ffmpegDirectory;
        }
        else
        {
            if (!File.Exists(localFfmpegPath))
            {
                throw new FileNotFoundException(
                    $"No se encontró FFmpeg en: {localFfmpegPath}");
            }

            _ffmpegDirectory = toolsDirectory;
        }
    }

    public async Task DownloadAsync(
        string url,
        MediaFormat format,
        string outputDirectory,
        IProgress<DownloadProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ValidateArguments(
            url,
            format,
            outputDirectory);

        Directory.CreateDirectory(outputDirectory);

        ProcessStartInfo startInfo =
            CreateProcessStartInfo(
                url,
                format,
                outputDirectory);

        using var process = new Process
        {
            StartInfo = startInfo
        };

        var errorBuilder = new StringBuilder();

        try
        {
            if (!process.Start())
            {
                throw new InvalidOperationException(
                    "No se pudo iniciar yt-dlp.");
            }

            /*
             * IMPORTANTE:
             *
             * stdout, stderr y WaitForExitAsync
             * se ejecutan al mismo tiempo.
             *
             * No usamos EndOfStream.
             */
            Task stdoutTask = ReadOutputAsync(
                process.StandardOutput,
                progress,
                cancellationToken);

            Task stderrTask = ReadErrorAsync(
                process.StandardError,
                progress,
                errorBuilder,
                cancellationToken);

            Task exitTask =
                process.WaitForExitAsync(
                    cancellationToken);

            await Task.WhenAll(
                stdoutTask,
                stderrTask,
                exitTask);

            if (process.ExitCode != 0)
            {
                string error =
                    errorBuilder.ToString().Trim();

                if (string.IsNullOrWhiteSpace(error))
                {
                    error =
                        $"yt-dlp terminó con código {process.ExitCode}.";
                }

                throw new InvalidOperationException(error);
            }

            progress?.Report(
                new DownloadProgress
                {
                    Percentage = 100,
                    Status = "Descarga completada"
                });
        }
        catch (OperationCanceledException)
        {
            KillProcess(process);

            throw;
        }
        catch
        {
            KillProcess(process);

            throw;
        }
    }

    private ProcessStartInfo CreateProcessStartInfo(
        string url,
        MediaFormat format,
        string outputDirectory)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = _ytDlpPath,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        startInfo.ArgumentList.Add("--newline");
        startInfo.ArgumentList.Add("--progress");
        startInfo.ArgumentList.Add("--no-playlist");

        // FFmpeg
        startInfo.ArgumentList.Add(
            "--ffmpeg-location");

        startInfo.ArgumentList.Add(
            _ffmpegDirectory);

        // Progreso estructurado
        startInfo.ArgumentList.Add(
            "--progress-template");

        startInfo.ArgumentList.Add(
            $"{ProgressPrefix}" +
            "%(progress._percent_str)s|" +
            "%(progress._speed_str)s|" +
            "%(progress._eta_str)s|" +
            "%(progress.status)s");

        // Formato
        startInfo.ArgumentList.Add("-f");

        string formatSelector;

        if (format.HasVideo && !format.HasAudio)
        {
            if (string.Equals(
                format.Extension,
                "mp4",
                StringComparison.OrdinalIgnoreCase))
            {
                formatSelector =
                    $"{format.FormatId}+bestaudio[ext=m4a]/" +
                    $"{format.FormatId}+bestaudio/" +
                    "best";
            }
            else if (string.Equals(
                format.Extension,
                "webm",
                StringComparison.OrdinalIgnoreCase))
            {
                formatSelector =
                    $"{format.FormatId}+bestaudio[ext=webm]/" +
                    $"{format.FormatId}+bestaudio/" +
                    "best";
            }
            else
            {
                formatSelector =
                    $"{format.FormatId}+bestaudio/best";
            }
        }
        else
        {
            formatSelector =
                format.FormatId;
        }

        startInfo.ArgumentList.Add(
            formatSelector);

        // Contenedor final
        if (format.HasVideo)
        {
            startInfo.ArgumentList.Add(
                "--merge-output-format");

            startInfo.ArgumentList.Add(
                string.Equals(
                    format.Extension,
                    "webm",
                    StringComparison.OrdinalIgnoreCase)
                    ? "webm"
                    : "mp4");
        }

        // Nombre final
        startInfo.ArgumentList.Add("-o");

        startInfo.ArgumentList.Add(
            Path.Combine(
                outputDirectory,
                "%(title)s.%(ext)s"));

        startInfo.ArgumentList.Add(url);

        return startInfo;
    }

    private static async Task ReadOutputAsync(
        StreamReader reader,
        IProgress<DownloadProgress>? progress,
        CancellationToken cancellationToken)
    {
        while (true)
        {
            cancellationToken
                .ThrowIfCancellationRequested();

            string? line =
                await reader.ReadLineAsync(
                    cancellationToken);

            if (line is null)
                break;

            if (string.IsNullOrWhiteSpace(line))
                continue;

            Debug.WriteLine(
                $"[yt-dlp stdout] {line}");

            ParseProgress(
                line,
                progress);
        }
    }

    private static async Task ReadErrorAsync(
        StreamReader reader,
        IProgress<DownloadProgress>? progress,
        StringBuilder errorBuilder,
        CancellationToken cancellationToken)
    {
        while (true)
        {
            cancellationToken
                .ThrowIfCancellationRequested();

            string? line =
                await reader.ReadLineAsync(
                    cancellationToken);

            if (line is null)
                break;

            if (string.IsNullOrWhiteSpace(line))
                continue;

            Debug.WriteLine(
                $"[yt-dlp stderr] {line}");

            /*
             * yt-dlp puede mandar mensajes de progreso
             * también por stderr, así que intentamos
             * parsearlos igualmente.
             */
            if (ParseProgress(
                    line,
                    progress))
            {
                continue;
            }

            /*
             * Guardamos stderr para devolver un error
             * útil si yt-dlp termina con código != 0.
             */
            errorBuilder.AppendLine(line);
        }
    }

    private static bool ParseProgress(
        string line,
        IProgress<DownloadProgress>? progress)
    {
        /*
         * Buscamos el prefijo incluso si yt-dlp
         * añade algo delante.
         */
        int prefixIndex =
            line.IndexOf(
                ProgressPrefix,
                StringComparison.Ordinal);

        if (prefixIndex < 0)
            return false;

        string progressLine =
            line[prefixIndex..];

        string[] parts =
            progressLine.Split('|');

        /*
         * Esperamos:
         *
         * 0 = DLPROGRESS
         * 1 = porcentaje
         * 2 = velocidad
         * 3 = ETA
         * 4 = estado
         */
        if (parts.Length < 5)
            return false;

        string percentageText =
            parts[1]
                .Trim()
                .TrimEnd('%');

        if (!double.TryParse(
                percentageText,
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out double percentage))
        {
            return false;
        }

        string speed =
            NormalizeValue(parts[2]);

        string eta =
            NormalizeValue(parts[3]);

        string status =
            NormalizeValue(parts[4]);

        progress?.Report(
            new DownloadProgress
            {
                Percentage = percentage,
                Speed = speed,
                Eta = eta,
                Status = status
            });

        return true;
    }

    private static string NormalizeValue(
        string value)
    {
        string result =
            value.Trim();

        if (string.Equals(
                result,
                "NA",
                StringComparison.OrdinalIgnoreCase))
        {
            return string.Empty;
        }

        return result;
    }

    private static void ValidateArguments(
        string url,
        MediaFormat format,
        string outputDirectory)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            throw new ArgumentException(
                "La URL no puede estar vacía.",
                nameof(url));
        }

        if (!Uri.TryCreate(
                url,
                UriKind.Absolute,
                out Uri? uri) ||
            (uri.Scheme != Uri.UriSchemeHttp &&
             uri.Scheme != Uri.UriSchemeHttps))
        {
            throw new ArgumentException(
                "La URL no es válida.",
                nameof(url));
        }

        ArgumentNullException
            .ThrowIfNull(format);

        if (string.IsNullOrWhiteSpace(
                format.FormatId))
        {
            throw new ArgumentException(
                "El formato seleccionado no tiene FormatId.",
                nameof(format));
        }

        if (string.IsNullOrWhiteSpace(
                outputDirectory))
        {
            throw new ArgumentException(
                "La carpeta de salida no puede estar vacía.",
                nameof(outputDirectory));
        }
    }

    private static void KillProcess(
        Process process)
    {
        try
        {
            if (!process.HasExited)
            {
                process.Kill(
                    entireProcessTree: true);
            }
        }
        catch
        {
            // No ocultamos la excepción original
            // si el proceso ya terminó mientras
            // intentábamos cancelarlo.
        }
    }
}