using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace YtDlpWrapper.Models
{
    public partial class VideoInfo : ObservableObject
    {
        [ObservableProperty]
        private string title = string.Empty;

        [ObservableProperty]
        private string duration = "00:00:00";

        [ObservableProperty]
        private string filePath = string.Empty;

        [ObservableProperty]
        private TimeSpan totalDuration = TimeSpan.Zero;

        [ObservableProperty]
        private TimeSpan currentPosition = TimeSpan.Zero;

        [ObservableProperty]
        private bool isPlaying;

        [ObservableProperty]
        private double volume = 1.0;

        [ObservableProperty]
        private bool isMuted;

        [ObservableProperty]
        private string currentTime = "00:00:00";

        [ObservableProperty]
        private double currentPositionSeconds = 0;

        [ObservableProperty]
        private double totalDurationSeconds = 0;

        public string FormattedCurrentPosition => CurrentPosition.ToString(@"hh\:mm\:ss");
        public string FormattedTotalDuration => TotalDuration.TotalSeconds > 0 ? TotalDuration.ToString(@"hh\:mm\:ss") : "Unknown";
        public double PlaybackProgress => TotalDuration.TotalSeconds > 0 ? CurrentPosition.TotalSeconds / TotalDuration.TotalSeconds : 0;
    }
} 