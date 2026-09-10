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
    // TIPO DE SALIDA
    // ==========================================

    public ObservableCollection<OutputFormat> OutputFormats { get; } =
    [
        new OutputFormat
        {
            Name = "Video",
            Extension = "mp4",
            IsAudioOnly = false
        },

        new OutputFormat
        {
            Name = "Audio",
            Extension = "mp3",
            IsAudioOnly = true
        }
    ];

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(DownloadCommand))]
    private OutputFormat? selectedOutputFormat;

    // ==========================================
    // CALIDADES
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
    // CAMBIO DE TIPO DE SALIDA
    // ==========================================

    partial void OnSelectedOutputFormatChanged(OutputFormat? value)
    {
        RefreshQualities();
    }

    private void RefreshQualities()
    {
        Qualities.Clear();
        SelectedQuality = null;

        if (SelectedOutputFormat is null)
            return;

        IEnumerable<MediaFormat> availableFormats;

        if (SelectedOutputFormat.IsAudioOnly)
        {
            // Para MP3 mostramos únicamente pistas con audio.
            availableFormats = Formats
                .Where(format => format.HasAudio)
                .OrderByDescending(format =>
                    format.AudioBitrate ?? 0);
        }
        else
        {
            // Para MP4 mostramos formatos que contienen video.
            availableFormats = Formats
                .Where(format => format.HasVideo)
                .OrderByDescending(format =>
                    format.Height ?? 0);
        }

        foreach (MediaFormat format in availableFormats)
        {
            Qualities.Add(format);
        }

        SelectedQuality = Qualities.FirstOrDefault();
    }

    // ==========================================
    // ANALIZAR
    // ==========================================

    [RelayCommand(CanExecute = nameof(CanAnalyze))]
    private async Task AnalyzeAsync()
    {
        IsBusy = true;
        StatusMessage = "Analizando...";

        MediaDetailsVisibility = Visibility.Collapsed;

        Formats.Clear();
        Qualities.Clear();

        SelectedQuality = null;
        SelectedOutputFormat = null;

        DownloadProgressVisibility = Visibility.Collapsed;
        DownloadPercentage = 0;
        DownloadStatus = string.Empty;

        try
        {
            MediaInfo media =
                await _analyzeMediaUseCase.ExecuteAsync(Url);

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

            // Seleccionamos MP4 por defecto.
            // Esto ejecutará OnSelectedOutputFormatChanged()
            // y llenará automáticamente Qualities.
            SelectedOutputFormat =
                OutputFormats.FirstOrDefault();

            MediaDetailsVisibility = Visibility.Visible;

            StatusMessage =
                Formats.Count > 0
                    ? $"Video analizado correctamente. {Formats.Count} formatos encontrados."
                    : "Video analizado, pero no se encontraron formatos.";
        }
        catch (Exception ex)
        {
            MediaDetailsVisibility = Visibility.Collapsed;

            StatusMessage = ex.Message;

            VideoTitle =
                "No se pudo analizar el video";

            Uploader =
                "Canal / Autor";

            DurationText =
                "--:--";

            ThumbnailUrl = null;

            Formats.Clear();
            Qualities.Clear();

            SelectedOutputFormat = null;
            SelectedQuality = null;
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
        if (SelectedOutputFormat is null ||
            SelectedQuality is null)
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

            /*
             * POR AHORA:
             *
             * Seguimos enviando SelectedQuality porque tu
             * IMediaDownloader actual recibe MediaFormat.
             *
             * En el siguiente cambio habrá que enviar también
             * SelectedOutputFormat para poder diferenciar:
             *
             * MP4 -> descargar/combinar video + audio
             * MP3 -> descargar audio + convertir con FFmpeg
             */

            await _downloadMediaUseCase.ExecuteAsync(
                Url,
                SelectedQuality,
                downloadsFolder,
                progress);

            DownloadPercentage = 100;
            DownloadStatus = "Descarga completada.";

            DownloadProgressVisibility = Visibility.Collapsed;
            IsDownloadCompletedMessageOpen = true;
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
            SelectedOutputFormat is not null &&
            SelectedQuality is not null &&
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