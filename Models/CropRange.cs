using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace YtDlpWrapper.Models
{
    public partial class CropRange : ObservableObject
    {
        [ObservableProperty]
        private TimeSpan startTime = TimeSpan.Zero;

        [ObservableProperty]
        private TimeSpan endTime = TimeSpan.Zero;

        [ObservableProperty]
        private bool isValid;

        public string FormattedStartTime => StartTime.ToString(@"hh\:mm\:ss");
        public string FormattedEndTime => EndTime.ToString(@"hh\:mm\:ss");
        public TimeSpan Duration => EndTime - StartTime;
        public string FormattedDuration => Duration.ToString(@"hh\:mm\:ss");

        partial void OnStartTimeChanged(TimeSpan value)
        {
            ValidateRange();
        }

        partial void OnEndTimeChanged(TimeSpan value)
        {
            ValidateRange();
        }

        private void ValidateRange()
        {
            IsValid = StartTime < EndTime && StartTime >= TimeSpan.Zero;
        }

        public void SetRange(TimeSpan start, TimeSpan end)
        {
            StartTime = start;
            EndTime = end;
        }
    }
} 