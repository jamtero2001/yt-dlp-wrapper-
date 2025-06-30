using System.Windows;
using YtDlpWrapper.ViewModels;
using System.ComponentModel;
using System.Windows.Threading;
using System.Windows.Controls.Primitives;
using YtDlpWrapper.Models;
using System.Windows.Controls;
using System.Windows.Input;

namespace YtDlpWrapper.Views
{
    public partial class MainWindow : Window
    {
        private readonly MainViewModel _viewModel;
        private readonly DispatcherTimer _videoTimer;
        private bool _isSliderDragging = false;
        private VideoInfo? _currentVideoSubscribed; // Keep track of the video we're subscribed to

        public MainWindow()
        {
            InitializeComponent();
            _viewModel = new MainViewModel();
            DataContext = _viewModel;
            
            // Subscribe to property changes
            _viewModel.PropertyChanged += ViewModel_PropertyChanged;
            
            // Setup video timer
            _videoTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(100)
            };
            _videoTimer.Tick += VideoTimer_Tick;
        }

        private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine($"Property changed: {e.PropertyName}");
            
            if (e.PropertyName == nameof(MainViewModel.CurrentVideo))
            {
                System.Diagnostics.Debug.WriteLine("CurrentVideo property changed!");
                
                // Unsubscribe from the old video object
                if (_currentVideoSubscribed != null)
                {
                    _currentVideoSubscribed.PropertyChanged -= CurrentVideo_PropertyChanged;
                }

                if (!string.IsNullOrEmpty(_viewModel.CurrentVideo?.FilePath))
                {
                    System.Diagnostics.Debug.WriteLine($"Setting video source to: {_viewModel.CurrentVideo.FilePath}");
                    
                    try
                    {
                        var uri = new Uri(_viewModel.CurrentVideo.FilePath);
                        System.Diagnostics.Debug.WriteLine($"Created URI: {uri}");
                        
                        VideoPlayer.Source = uri;
                        System.Diagnostics.Debug.WriteLine("VideoPlayer.Source set successfully");
                        
                        // Set initial volume
                        VideoPlayer.Volume = _viewModel.CurrentVideo.Volume;
                        
                        // Force the MediaElement to load
                        VideoPlayer.Play();
                        System.Diagnostics.Debug.WriteLine("VideoPlayer.Play() called");

                        // Subscribe to the new video object's property changes
                        _viewModel.CurrentVideo.PropertyChanged += CurrentVideo_PropertyChanged;
                        _currentVideoSubscribed = _viewModel.CurrentVideo;
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error setting video source: {ex.Message}");
                        MessageBox.Show($"Error loading video: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("CurrentVideo.FilePath is null or empty");
                }
            }
        }

        private void CurrentVideo_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            // When the Volume property on the VideoInfo object changes, update the player
            if (e.PropertyName == nameof(VideoInfo.Volume))
            {
                if (sender is VideoInfo videoInfo)
                {
                    VideoPlayer.Volume = videoInfo.Volume;
                }
            }
        }

        private void VideoPlayer_MediaOpened(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("MediaOpened event fired!");
            System.Diagnostics.Debug.WriteLine($"Video duration: {VideoPlayer.NaturalDuration}");
            System.Diagnostics.Debug.WriteLine($"Video source: {VideoPlayer.Source}");
            
            // Update the time bar with video duration
            if (VideoPlayer.NaturalDuration.HasTimeSpan)
            {
                var duration = VideoPlayer.NaturalDuration.TimeSpan;
                System.Diagnostics.Debug.WriteLine($"Video duration: {duration}");
                System.Diagnostics.Debug.WriteLine($"Video successfully loaded and ready to play");
                
                // Update the ViewModel with video duration
                _viewModel.CurrentVideo.TotalDuration = duration;
                _viewModel.CurrentVideo.TotalDurationSeconds = duration.TotalSeconds;
                _viewModel.CurrentVideo.Duration = duration.ToString(@"hh\:mm\:ss");
                
                // Set crop range to full video
                _viewModel.CropRange.SetRange(TimeSpan.Zero, duration);
                
                // Start the timer to update position
                _videoTimer.Start();
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("Video duration not available");
            }
        }

        private void VideoPlayer_MediaFailed(object sender, ExceptionRoutedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine($"MediaFailed event fired! Error: {e.ErrorException?.Message}");
            // System.Diagnostics.Debug.WriteLine($"Error source: {e.ErrorException?.Source}");
            // System.Diagnostics.Debug.WriteLine($"Error stack trace: {e.ErrorException?.StackTrace}");
        }

        private void VideoTimer_Tick(object? sender, EventArgs e)
        {
            if (VideoPlayer.NaturalDuration.HasTimeSpan && !_isSliderDragging)
            {
                var position = VideoPlayer.Position;
                _viewModel.CurrentVideo.CurrentPosition = position;
                _viewModel.CurrentVideo.CurrentPositionSeconds = position.TotalSeconds;
                _viewModel.CurrentVideo.CurrentTime = position.ToString(@"hh\:mm\:ss");
            }
        }

        private void VideoSlider_DragStarted(object sender, DragStartedEventArgs e)
        {
            _isSliderDragging = true;
        }

        private void VideoSlider_DragCompleted(object sender, DragCompletedEventArgs e)
        {
            if (VideoPlayer.NaturalDuration.HasTimeSpan)
            {
                var newPosition = TimeSpan.FromSeconds(VideoSlider.Value);
                VideoPlayer.Position = newPosition;
                _viewModel.CurrentVideo.CurrentPosition = newPosition;
                _viewModel.CurrentVideo.CurrentPositionSeconds = newPosition.TotalSeconds;
                _viewModel.CurrentVideo.CurrentTime = newPosition.ToString(@"hh\:mm\:ss");
            }
            _isSliderDragging = false;
        }

        private void PlayPauseButton_Click(object sender, RoutedEventArgs e)
        {
            if (VideoPlayer.Position >= VideoPlayer.NaturalDuration)
            {
                VideoPlayer.Position = TimeSpan.Zero;
            }

            if (_viewModel.CurrentVideo.IsPlaying)
            {
                VideoPlayer.Pause();
                _viewModel.CurrentVideo.IsPlaying = false;
                _videoTimer.Stop();
            }
            else
            {
                VideoPlayer.Play();
                _viewModel.CurrentVideo.IsPlaying = true;
                _videoTimer.Start();
            }
        }

        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            VideoPlayer.Stop();
            _viewModel.CurrentVideo.IsPlaying = false;
            _videoTimer.Stop();
            _viewModel.CurrentVideo.CurrentPosition = TimeSpan.Zero;
            _viewModel.CurrentVideo.CurrentPositionSeconds = 0;
            _viewModel.CurrentVideo.CurrentTime = "00:00:00";
        }

        private void VideoSlider_PreviewMouseLeftButtonDown(object? sender, MouseButtonEventArgs e)
        {
            if (sender is Slider slider)
            {
                // This calculates the position of the click relative to the slider's track.
                Point position = e.GetPosition(slider);
                double ratio = position.X / slider.ActualWidth;
                double newValue = ratio * slider.Maximum;

                // Set the slider value and update the video position
                slider.Value = newValue;
                var newPosition = TimeSpan.FromSeconds(newValue);
                VideoPlayer.Position = newPosition;
                _viewModel.CurrentVideo.CurrentPosition = newPosition;
            }
        }

        protected override void OnClosed(System.EventArgs e)
        {
            base.OnClosed(e);
            _viewModel?.Dispose();
        }
    }
} 