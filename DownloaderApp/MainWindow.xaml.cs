using DownloaderApp.Application.UseCases.AnalyzeMedia;
using DownloaderApp.Application.UseCases.DownloadMedia;
using DownloaderApp.Infrastructure.YtDlp;
using DownloaderApp.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Graphics;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace DownloaderApp
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            AppWindow.Resize(new SizeInt32(1050, 760));

            var analyzer =
                new YtDlpMediaAnalyzer();

            var downloader =
                new YtDlpMediaDownloader();

            var analyzeMediaUseCase =
                new AnalyzeMediaUseCase(
                    analyzer);

            var downloadMediaUseCase =
                new DownloadMediaUseCase(
                    downloader);

            RootGrid.DataContext =
                new MainViewModel(
                    analyzeMediaUseCase,
                    downloadMediaUseCase);
        }
    }
}
