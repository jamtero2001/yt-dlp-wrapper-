using System;
using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;

namespace YtDlpWrapper.Models
{
    public partial class DownloadInfo : ObservableObject
    {
        [ObservableProperty]
        private string url = string.Empty;

        [ObservableProperty]
        private string quality = "Best Quality";

        [ObservableProperty]
        private string format = "mp4";

        [ObservableProperty]
        private string downloadDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        [ObservableProperty]
        private double progress;

        [ObservableProperty]
        private string progressText = "0%";

        [ObservableProperty]
        private string speed = "0 MB/s";

        [ObservableProperty]
        private string status = "Ready";

        [ObservableProperty]
        private bool isDownloading;

        [ObservableProperty]
        private string fileName = string.Empty;

        [ObservableProperty]
        private TimeSpan estimatedTimeRemaining = TimeSpan.Zero;

        public List<string> AvailableQualities => new() { "Best Quality", "1080p", "720p", "480p", "360p", "audio-only" };
        public List<string> AvailableFormats => new() { "mp4", "webm", "mkv" };
    }
} 