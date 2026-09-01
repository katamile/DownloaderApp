using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DownloaderApp.Application.Abstractions;
using DownloaderApp.Domain.Models;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace DownloaderApp.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly IMediaAnalyzer _mediaAnalyzer;
    private readonly IMediaDownloader _mediaDownloader;

    public MainViewModel(
        IMediaAnalyzer mediaAnalyzer,
        IMediaDownloader mediaDownloader)
    {
        _mediaAnalyzer = mediaAnalyzer;
        _mediaDownloader = mediaDownloader;
    }

    // ==========================================
    // URL
    // ==========================================

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AnalyzeCommand))]
    private string url = string.Empty;

    // ==========================================
    // ESTADO DEL ANALIZADOR
    // ==========================================

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AnalyzeCommand))]
    private bool isBusy;

    [ObservableProperty]
    private string statusMessage = string.Empty;

    // ==========================================
    // INFORMACIÓN DEL VIDEO
    // ==========================================

    [ObservableProperty]
    private string videoTitle =
        "Aquí aparecerá el título del video";

    [ObservableProperty]
    private string uploader =
        "Canal / Autor";

    [ObservableProperty]
    private string durationText =
        "--:--";

    [ObservableProperty]
    private string? thumbnailUrl;

    // ==========================================
    // FORMATOS
    // ==========================================

    public ObservableCollection<MediaFormat> Formats { get; } = [];

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(DownloadCommand))]
    private MediaFormat? selectedFormat;

    // ==========================================
    // DESCARGA
    // ==========================================

    [ObservableProperty]
    private double downloadPercentage;

    [ObservableProperty]
    private string downloadStatus = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(DownloadCommand))]
    private bool isDownloading;

    // ==========================================
    // ANALIZAR
    // ==========================================

    [RelayCommand(CanExecute = nameof(CanAnalyze))]
    private async Task AnalyzeAsync()
    {
        IsBusy = true;
        StatusMessage = "Analizando...";

        Formats.Clear();
        SelectedFormat = null;

        try
        {
            MediaInfo media =
                await _mediaAnalyzer.AnalyzeAsync(Url);

            VideoTitle = media.Title;

            Uploader =
                string.IsNullOrWhiteSpace(media.Uploader)
                    ? "Autor desconocido"
                    : media.Uploader;

            ThumbnailUrl = media.ThumbnailUrl;

            DurationText =
                FormatDuration(media.Duration);

            var usefulFormats = media.Formats
                .Where(format =>
                    format.HasVideo ||
                    format.HasAudio)
                .OrderByDescending(format =>
                    format.Height ?? 0)
                .ThenByDescending(format =>
                    format.AudioBitrate ?? 0);

            foreach (MediaFormat format in usefulFormats)
            {
                Formats.Add(format);
            }

            SelectedFormat =
                Formats.FirstOrDefault();

            StatusMessage =
                Formats.Count > 0
                    ? $"Video analizado correctamente. {Formats.Count} formatos encontrados."
                    : "Video analizado, pero no se encontraron formatos.";
        }
        catch (Exception ex)
        {
            StatusMessage = ex.Message;

            VideoTitle =
                "No se pudo analizar el video";

            Uploader =
                "Canal / Autor";

            DurationText =
                "--:--";

            ThumbnailUrl = null;

            Formats.Clear();
            SelectedFormat = null;
        }
        finally
        {
            IsBusy = false;
        }
    }

    private bool CanAnalyze()
    {
        if (IsBusy || IsDownloading)
            return false;

        if (string.IsNullOrWhiteSpace(Url))
            return false;

        return Uri.TryCreate(
                   Url,
                   UriKind.Absolute,
                   out Uri? uri)
               &&
               (uri.Scheme == Uri.UriSchemeHttp ||
                uri.Scheme == Uri.UriSchemeHttps);
    }

    // ==========================================
    // DESCARGAR
    // ==========================================

    [RelayCommand(CanExecute = nameof(CanDownload))]
    private async Task DownloadAsync()
    {
        if (SelectedFormat is null)
            return;

        IsDownloading = true;
        DownloadPercentage = 0;
        DownloadStatus = "Preparando descarga...";

        try
        {
            string downloadsFolder = Path.Combine(
                Environment.GetFolderPath(
                    Environment.SpecialFolder.UserProfile),
                "Downloads");

            var progress =
                new Progress<DownloadProgress>(downloadProgress =>
                {
                    DownloadPercentage =
                        downloadProgress.Percentage;

                    DownloadStatus =
                        $"Descargando... {downloadProgress.Percentage:0.0}%";
                });

            await _mediaDownloader.DownloadAsync(
                Url,
                SelectedFormat,
                downloadsFolder,
                progress);

            DownloadPercentage = 100;

            DownloadStatus =
                "Descarga completada.";
        }
        catch (Exception ex)
        {
            DownloadStatus =
                $"Error: {ex.Message}";
        }
        finally
        {
            IsDownloading = false;
        }
    }

    private bool CanDownload()
    {
        return
            SelectedFormat is not null &&
            !IsDownloading &&
            !IsBusy;
    }

    // ==========================================
    // HELPERS
    // ==========================================

    private static string FormatDuration(double? seconds)
    {
        if (seconds is null)
            return "--:--";

        TimeSpan time =
            TimeSpan.FromSeconds(seconds.Value);

        if (time.TotalHours >= 1)
        {
            return
                $"{(int)time.TotalHours}:{time.Minutes:00}:{time.Seconds:00}";
        }

        return
            $"{(int)time.TotalMinutes}:{time.Seconds:00}";
    }
}