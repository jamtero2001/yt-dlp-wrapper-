using CommunityToolkit.Mvvm.Input;
using MaterialDesignThemes.Wpf;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using YtDlpWrapper.Models;

namespace YtDlpWrapper.Controls
{
    public partial class CustomTimeBar : UserControl
    {
        private bool _isDraggingStart;
        private bool _isDraggingEnd;
        private bool _isDraggingPlayhead;
        private Point _lastMousePosition;

        // Properties for binding
        public static readonly DependencyProperty CurrentTimeProperty =
            DependencyProperty.Register("CurrentTime", typeof(string), typeof(CustomTimeBar), new PropertyMetadata("00:00:00"));

        public static readonly DependencyProperty TotalTimeProperty =
            DependencyProperty.Register("TotalTime", typeof(string), typeof(CustomTimeBar), new PropertyMetadata("00:00:00"));

        public static readonly DependencyProperty ProgressWidthProperty =
            DependencyProperty.Register("ProgressWidth", typeof(double), typeof(CustomTimeBar), new PropertyMetadata(0.0));

        public static readonly DependencyProperty PlayheadMarginProperty =
            DependencyProperty.Register("PlayheadMargin", typeof(Thickness), typeof(CustomTimeBar), new PropertyMetadata(new Thickness()));

        public static readonly DependencyProperty StartThumbMarginProperty =
            DependencyProperty.Register("StartThumbMargin", typeof(Thickness), typeof(CustomTimeBar), new PropertyMetadata(new Thickness()));

        public static readonly DependencyProperty EndThumbMarginProperty =
            DependencyProperty.Register("EndThumbMargin", typeof(Thickness), typeof(CustomTimeBar), new PropertyMetadata(new Thickness()));

        public static readonly DependencyProperty CropRangeMarginProperty =
            DependencyProperty.Register("CropRangeMargin", typeof(Thickness), typeof(CustomTimeBar), new PropertyMetadata(new Thickness()));

        public static readonly DependencyProperty CropRangeWidthProperty =
            DependencyProperty.Register("CropRangeWidth", typeof(double), typeof(CustomTimeBar), new PropertyMetadata(0.0));

        public static readonly DependencyProperty StartTimeLabelProperty =
            DependencyProperty.Register("StartTimeLabel", typeof(string), typeof(CustomTimeBar), new PropertyMetadata("00:00:00"));

        public static readonly DependencyProperty EndTimeLabelProperty =
            DependencyProperty.Register("EndTimeLabel", typeof(string), typeof(CustomTimeBar), new PropertyMetadata("00:00:00"));

        public static readonly DependencyProperty StartTimeMarginProperty =
            DependencyProperty.Register("StartTimeMargin", typeof(Thickness), typeof(CustomTimeBar), new PropertyMetadata(new Thickness()));

        public static readonly DependencyProperty EndTimeMarginProperty =
            DependencyProperty.Register("EndTimeMargin", typeof(Thickness), typeof(CustomTimeBar), new PropertyMetadata(new Thickness()));

        public static readonly DependencyProperty PlayPauseIconProperty =
            DependencyProperty.Register("PlayPauseIcon", typeof(PackIconKind), typeof(CustomTimeBar), new PropertyMetadata(PackIconKind.Play));

        public static readonly DependencyProperty VideoInfoProperty =
            DependencyProperty.Register("VideoInfo", typeof(VideoInfo), typeof(CustomTimeBar), 
                new PropertyMetadata(null, OnVideoInfoChanged));

        public static readonly DependencyProperty CropRangeProperty =
            DependencyProperty.Register("CropRange", typeof(CropRange), typeof(CustomTimeBar), 
                new PropertyMetadata(null, OnCropRangeChanged));

        // Property accessors
        public string CurrentTime
        {
            get => (string)GetValue(CurrentTimeProperty);
            set => SetValue(CurrentTimeProperty, value);
        }

        public string TotalTime
        {
            get => (string)GetValue(TotalTimeProperty);
            set => SetValue(TotalTimeProperty, value);
        }

        public double ProgressWidth
        {
            get => (double)GetValue(ProgressWidthProperty);
            set => SetValue(ProgressWidthProperty, value);
        }

        public Thickness PlayheadMargin
        {
            get => (Thickness)GetValue(PlayheadMarginProperty);
            set => SetValue(PlayheadMarginProperty, value);
        }

        public Thickness StartThumbMargin
        {
            get => (Thickness)GetValue(StartThumbMarginProperty);
            set => SetValue(StartThumbMarginProperty, value);
        }

        public Thickness EndThumbMargin
        {
            get => (Thickness)GetValue(EndThumbMarginProperty);
            set => SetValue(EndThumbMarginProperty, value);
        }

        public Thickness CropRangeMargin
        {
            get => (Thickness)GetValue(CropRangeMarginProperty);
            set => SetValue(CropRangeMarginProperty, value);
        }

        public double CropRangeWidth
        {
            get => (double)GetValue(CropRangeWidthProperty);
            set => SetValue(CropRangeWidthProperty, value);
        }

        public string StartTimeLabel
        {
            get => (string)GetValue(StartTimeLabelProperty);
            set => SetValue(StartTimeLabelProperty, value);
        }

        public string EndTimeLabel
        {
            get => (string)GetValue(EndTimeLabelProperty);
            set => SetValue(EndTimeLabelProperty, value);
        }

        public Thickness StartTimeMargin
        {
            get => (Thickness)GetValue(StartTimeMarginProperty);
            set => SetValue(StartTimeMarginProperty, value);
        }

        public Thickness EndTimeMargin
        {
            get => (Thickness)GetValue(EndTimeMarginProperty);
            set => SetValue(EndTimeMarginProperty, value);
        }

        public PackIconKind PlayPauseIcon
        {
            get => (PackIconKind)GetValue(PlayPauseIconProperty);
            set => SetValue(PlayPauseIconProperty, value);
        }

        public VideoInfo? VideoInfo
        {
            get => (VideoInfo?)GetValue(VideoInfoProperty);
            set => SetValue(VideoInfoProperty, value);
        }

        public CropRange? CropRange
        {
            get => (CropRange?)GetValue(CropRangeProperty);
            set => SetValue(CropRangeProperty, value);
        }

        public IRelayCommand PlayPauseCommand { get; }
        public IRelayCommand StopCommand { get; }
        public IRelayCommand SetStartTimeCommand { get; }
        public IRelayCommand SetEndTimeCommand { get; }
        public IRelayCommand ResetRangeCommand { get; }

        public CustomTimeBar()
        {
            InitializeComponent();
            DataContext = this;

            PlayPauseCommand = new RelayCommand(PlayPause);
            StopCommand = new RelayCommand(Stop);
            SetStartTimeCommand = new RelayCommand(SetStartTime);
            SetEndTimeCommand = new RelayCommand(SetEndTime);
            ResetRangeCommand = new RelayCommand(ResetRange);

            // Setup mouse events
            StartThumb.MouseLeftButtonDown += StartThumb_MouseLeftButtonDown;
            EndThumb.MouseLeftButtonDown += EndThumb_MouseLeftButtonDown;
            Playhead.MouseLeftButtonDown += Playhead_MouseLeftButtonDown;
            ProgressContainer.MouseLeftButtonDown += ProgressContainer_MouseLeftButtonDown;

            Loaded += CustomTimeBar_Loaded;
        }

        private void CustomTimeBar_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateLayout();
        }

        private static void OnVideoInfoChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is CustomTimeBar timeBar)
            {
                timeBar.UpdateTimeDisplay();
            }
        }

        private static void OnCropRangeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is CustomTimeBar timeBar)
            {
                timeBar.UpdateCropRange();
            }
        }

        private void UpdateTimeDisplay()
        {
            if (VideoInfo != null)
            {
                CurrentTime = VideoInfo.FormattedCurrentPosition;
                TotalTime = VideoInfo.FormattedTotalDuration;
                
                var progress = VideoInfo.PlaybackProgress;
                ProgressWidth = progress * ProgressContainer.ActualWidth;
                PlayheadMargin = new Thickness(ProgressWidth - 8, 0, 0, 0);
            }
        }

        private void UpdateCropRange()
        {
            if (CropRange != null && VideoInfo != null && VideoInfo.TotalDuration.TotalSeconds > 0)
            {
                var totalWidth = ProgressContainer.ActualWidth;
                var startRatio = CropRange.StartTime.TotalSeconds / VideoInfo.TotalDuration.TotalSeconds;
                var endRatio = CropRange.EndTime.TotalSeconds / VideoInfo.TotalDuration.TotalSeconds;

                StartThumbMargin = new Thickness(startRatio * totalWidth - 10, 0, 0, 0);
                EndThumbMargin = new Thickness(endRatio * totalWidth - 10, 0, 0, 0);
                CropRangeMargin = new Thickness(startRatio * totalWidth, 0, 0, 0);
                CropRangeWidth = (endRatio - startRatio) * totalWidth;

                StartTimeLabel = CropRange.FormattedStartTime;
                EndTimeLabel = CropRange.FormattedEndTime;
                StartTimeMargin = new Thickness(startRatio * totalWidth - 15, 0, 0, 0);
                EndTimeMargin = new Thickness(endRatio * totalWidth - 15, 0, 0, 0);
            }
        }

        private void StartThumb_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _isDraggingStart = true;
            _lastMousePosition = e.GetPosition(ProgressContainer);
            StartThumb.CaptureMouse();
            e.Handled = true;
        }

        private void EndThumb_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _isDraggingEnd = true;
            _lastMousePosition = e.GetPosition(ProgressContainer);
            EndThumb.CaptureMouse();
            e.Handled = true;
        }

        private void Playhead_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _isDraggingPlayhead = true;
            _lastMousePosition = e.GetPosition(ProgressContainer);
            Playhead.CaptureMouse();
            e.Handled = true;
        }

        private void ProgressContainer_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (VideoInfo != null)
            {
                var position = e.GetPosition(ProgressContainer);
                var ratio = Math.Max(0, Math.Min(1, position.X / ProgressContainer.ActualWidth));
                var newTime = TimeSpan.FromSeconds(ratio * VideoInfo.TotalDuration.TotalSeconds);
                VideoInfo.CurrentPosition = newTime;
                UpdateTimeDisplay();
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);

            if (_isDraggingStart && VideoInfo != null && CropRange != null)
            {
                var position = e.GetPosition(ProgressContainer);
                var ratio = Math.Max(0, Math.Min(1, position.X / ProgressContainer.ActualWidth));
                var newTime = TimeSpan.FromSeconds(ratio * VideoInfo.TotalDuration.TotalSeconds);
                
                if (newTime < CropRange.EndTime)
                {
                    CropRange.StartTime = newTime;
                    UpdateCropRange();
                }
            }
            else if (_isDraggingEnd && VideoInfo != null && CropRange != null)
            {
                var position = e.GetPosition(ProgressContainer);
                var ratio = Math.Max(0, Math.Min(1, position.X / ProgressContainer.ActualWidth));
                var newTime = TimeSpan.FromSeconds(ratio * VideoInfo.TotalDuration.TotalSeconds);
                
                if (newTime > CropRange.StartTime)
                {
                    CropRange.EndTime = newTime;
                    UpdateCropRange();
                }
            }
            else if (_isDraggingPlayhead && VideoInfo != null)
            {
                var position = e.GetPosition(ProgressContainer);
                var ratio = Math.Max(0, Math.Min(1, position.X / ProgressContainer.ActualWidth));
                var newTime = TimeSpan.FromSeconds(ratio * VideoInfo.TotalDuration.TotalSeconds);
                VideoInfo.CurrentPosition = newTime;
                UpdateTimeDisplay();
            }
        }

        protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonUp(e);

            if (_isDraggingStart)
            {
                _isDraggingStart = false;
                StartThumb.ReleaseMouseCapture();
            }
            else if (_isDraggingEnd)
            {
                _isDraggingEnd = false;
                EndThumb.ReleaseMouseCapture();
            }
            else if (_isDraggingPlayhead)
            {
                _isDraggingPlayhead = false;
                Playhead.ReleaseMouseCapture();
            }
        }

        private void PlayPause()
        {
            if (VideoInfo != null)
            {
                VideoInfo.IsPlaying = !VideoInfo.IsPlaying;
                PlayPauseIcon = VideoInfo.IsPlaying ? PackIconKind.Pause : PackIconKind.Play;
            }
        }

        private void Stop()
        {
            if (VideoInfo != null)
            {
                VideoInfo.IsPlaying = false;
                VideoInfo.CurrentPosition = TimeSpan.Zero;
                PlayPauseIcon = PackIconKind.Play;
                UpdateTimeDisplay();
            }
        }

        private void SetStartTime()
        {
            if (VideoInfo != null && CropRange != null)
            {
                CropRange.StartTime = VideoInfo.CurrentPosition;
                UpdateCropRange();
            }
        }

        private void SetEndTime()
        {
            if (VideoInfo != null && CropRange != null)
            {
                CropRange.EndTime = VideoInfo.CurrentPosition;
                UpdateCropRange();
            }
        }

        private void ResetRange()
        {
            if (VideoInfo != null && CropRange != null)
            {
                CropRange.SetRange(TimeSpan.Zero, VideoInfo.TotalDuration);
                UpdateCropRange();
            }
        }
    }
} 