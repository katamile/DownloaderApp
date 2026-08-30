using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DownloaderApp.Application.Abstractions;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace DownloaderApp.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        private readonly IMediaAnalyzer _mediaAnalyzer;

        public MainViewModel(IMediaAnalyzer mediaAnalyzer)
        {
            _mediaAnalyzer = mediaAnalyzer;
        }

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(AnalyzeCommand))]
        private string url = string.Empty;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(AnalyzeCommand))]
        private bool isBusy;

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

        [ObservableProperty]
        private string statusMessage =
            string.Empty;

        [RelayCommand(CanExecute = nameof(CanAnalyze))]
        private async Task AnalyzeAsync()
        {
            IsBusy = true;
            StatusMessage = "Analizando...";

            try
            {
                var media = await _mediaAnalyzer.AnalyzeAsync(Url);

                VideoTitle = media.Title;

                Uploader = string.IsNullOrWhiteSpace(media.Uploader)
                    ? "Autor desconocido"
                    : media.Uploader;

                ThumbnailUrl = media.ThumbnailUrl;

                DurationText =
                    FormatDuration(media.Duration);

                StatusMessage = "Video analizado correctamente.";
            }
            catch (Exception ex)
            {
                StatusMessage = ex.Message;
            }
            finally
            {
                IsBusy = false;
            }
        }

        private bool CanAnalyze()
        {
            if (IsBusy)
                return false;

            return Uri.TryCreate(
                       Url,
                       UriKind.Absolute,
                       out Uri? uri)
                   &&
                   (uri.Scheme == Uri.UriSchemeHttp ||
                    uri.Scheme == Uri.UriSchemeHttps);
        }

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
}
