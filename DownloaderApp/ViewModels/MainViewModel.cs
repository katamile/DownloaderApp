using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DownloaderApp.Application.UseCases.AnalyzeMedia;
using DownloaderApp.Application.UseCases.DownloadMedia;
using DownloaderApp.Domain.Models;
using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace DownloaderApp.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly AnalyzeMediaUseCase _analyzeMediaUseCase;
    private readonly DownloadMediaUseCase _downloadMediaUseCase;

    public MainViewModel(
        AnalyzeMediaUseCase analyzeMediaUseCase,
        DownloadMediaUseCase downloadMediaUseCase)
    {
        _analyzeMediaUseCase = analyzeMediaUseCase;
        _downloadMediaUseCase = downloadMediaUseCase;
    }

    // ==========================================
    // VISIBILIDAD DE INFORMACIÓN
    // ==========================================

    [ObservableProperty]
    private Visibility mediaDetailsVisibility = Visibility.Collapsed;

    [ObservableProperty]
    private Visibility downloadProgressVisibility = Visibility.Collapsed;

    [ObservableProperty]
    private bool isDownloadCompletedMessageOpen;

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
    [NotifyCanExecuteChangedFor(nameof(DownloadCommand))]
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
    // FORMATOS ORIGINALES DE YT-DLP
    // ==========================================

    private ObservableCollection<MediaFormat> Formats { get; } = [];

    // ==========================================
    // TIPO DE DESCARGA
    // ==========================================

    public ObservableCollection<DownloadType> DownloadTypes { get; } =
    [
        DownloadType.Video,
        DownloadType.Audio
    ];

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(DownloadCommand))]
    private DownloadType selectedDownloadType = DownloadType.Video;

    // ==========================================
    // CALIDADES / FORMATOS DISPONIBLES
    // ==========================================

    public ObservableCollection<MediaFormat> Qualities { get; } = [];

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(DownloadCommand))]
    private MediaFormat? selectedQuality;

    // ==========================================
    // DESCARGA
    // ==========================================

    [ObservableProperty]
    private double downloadPercentage;

    [ObservableProperty]
    private string downloadStatus = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AnalyzeCommand))]
    [NotifyCanExecuteChangedFor(nameof(DownloadCommand))]
    private bool isDownloading;

    // ==========================================
    // CAMBIO DE TIPO DE DESCARGA
    // ==========================================

    partial void OnSelectedDownloadTypeChanged(
        DownloadType value)
    {
        RefreshQualities();
    }

    private void RefreshQualities()
    {
        Qualities.Clear();
        SelectedQuality = null;

        IEnumerable<MediaFormat> availableFormats;

        if (SelectedDownloadType == DownloadType.Audio)
        {
            // Solo streams de audio reales entregados por yt-dlp.
            availableFormats = Formats
                .Where(format =>
                    format.HasAudio &&
                    !format.HasVideo)
                .OrderByDescending(format =>
                    format.AudioBitrate ?? 0);
        }
        else
        {
            // Todos los formatos que contienen video.
            //
            // Algunos pueden traer audio incluido y otros
            // pueden ser video-only.
            //
            // YtDlpMediaDownloader se encargará de agregar
            // una pista de audio cuando sea necesario.
            availableFormats = Formats
                .Where(format =>
                    format.HasVideo)
                .OrderByDescending(format =>
                    format.Height ?? 0)
                .ThenByDescending(format =>
                    format.Fps ?? 0);
        }

        foreach (MediaFormat format in availableFormats)
        {
            Qualities.Add(format);
        }

        SelectedQuality =
            Qualities.FirstOrDefault();
    }

    // ==========================================
    // ANALIZAR
    // ==========================================

    [RelayCommand(CanExecute = nameof(CanAnalyze))]
    private async Task AnalyzeAsync()
    {
        IsBusy = true;
        StatusMessage = "Analizando...";

        MediaDetailsVisibility =
            Visibility.Collapsed;

        Formats.Clear();
        Qualities.Clear();

        SelectedQuality = null;

        DownloadProgressVisibility =
            Visibility.Collapsed;

        DownloadPercentage = 0;
        DownloadStatus = string.Empty;

        try
        {
            MediaInfo media =
                await _analyzeMediaUseCase.ExecuteAsync(
                    Url);

            VideoTitle = media.Title;

            Uploader =
                string.IsNullOrWhiteSpace(media.Uploader)
                    ? "Autor desconocido"
                    : media.Uploader;

            ThumbnailUrl =
                media.ThumbnailUrl;

            DurationText =
                FormatDuration(media.Duration);

            IEnumerable<MediaFormat> usefulFormats =
                media.Formats
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

            // Video por defecto.
            SelectedDownloadType =
                DownloadType.Video;

            // Como Video ya podría ser el valor actual,
            // llamamos explícitamente para asegurar que
            // la lista se refresque después del análisis.
            RefreshQualities();

            MediaDetailsVisibility =
                Visibility.Visible;

            StatusMessage =
                Formats.Count > 0
                    ? $"Video analizado correctamente. {Formats.Count} formatos encontrados."
                    : "Video analizado, pero no se encontraron formatos.";
        }
        catch (Exception ex)
        {
            MediaDetailsVisibility =
                Visibility.Collapsed;

            StatusMessage =
                ex.Message;

            VideoTitle =
                "No se pudo analizar el video";

            Uploader =
                "Canal / Autor";

            DurationText =
                "--:--";

            ThumbnailUrl = null;

            Formats.Clear();
            Qualities.Clear();

            SelectedQuality = null;
        }
        finally
        {
            IsBusy = false;
        }
    }

    private bool CanAnalyze()
    {
        if (IsBusy ||
            IsDownloading)
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(Url))
        {
            return false;
        }

        return Uri.TryCreate(
                   Url,
                   UriKind.Absolute,
                   out Uri? uri)
               &&
               (
                   uri.Scheme == Uri.UriSchemeHttp ||
                   uri.Scheme == Uri.UriSchemeHttps
               );
    }

    // ==========================================
    // DESCARGAR
    // ==========================================

    [RelayCommand(CanExecute = nameof(CanDownload))]
    private async Task DownloadAsync()
    {
        if (SelectedQuality is null)
        {
            return;
        }

        IsDownloading = true;

        IsDownloadCompletedMessageOpen = false;
        DownloadProgressVisibility = Visibility.Visible;

        DownloadPercentage = 0;
        DownloadStatus = "Preparando descarga...";

        try
        {
            string downloadsFolder =
                Path.Combine(
                    Environment.GetFolderPath(
                        Environment.SpecialFolder.UserProfile),
                    "Downloads");

            var progress =
                new Progress<DownloadProgress>(
                    downloadProgress =>
                    {
                        DownloadPercentage =
                            downloadProgress.Percentage;

                        DownloadStatus =
                            $"Descargando... {downloadProgress.Percentage:0.0}%";
                    });

            // El formato de salida será el mismo formato
            // que yt-dlp nos entregó.
            var outputFormat =
                new OutputFormat
                {
                    Name = SelectedDownloadType.ToString(),

                    Extension =
                        SelectedQuality.Extension
                        ?? string.Empty,

                    IsAudioOnly =
                        SelectedDownloadType ==
                        DownloadType.Audio
                };

            await _downloadMediaUseCase.ExecuteAsync(
                Url,
                SelectedQuality,
                outputFormat,
                downloadsFolder,
                progress);

            DownloadPercentage = 100;

            DownloadStatus =
                "Descarga completada.";

            DownloadProgressVisibility =
                Visibility.Collapsed;

            IsDownloadCompletedMessageOpen =
                true;
        }
        catch (Exception ex)
        {
            IsDownloadCompletedMessageOpen = false;

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
            SelectedQuality is not null &&
            !IsDownloading &&
            !IsBusy;
    }

    // ==========================================
    // HELPERS
    // ==========================================

    private static string FormatDuration(
        double? seconds)
    {
        if (seconds is null)
        {
            return "--:--";
        }

        TimeSpan time =
            TimeSpan.FromSeconds(
                seconds.Value);

        if (time.TotalHours >= 1)
        {
            return
                $"{(int)time.TotalHours}:{time.Minutes:00}:{time.Seconds:00}";
        }

        return
            $"{(int)time.TotalMinutes}:{time.Seconds:00}";
    }
}