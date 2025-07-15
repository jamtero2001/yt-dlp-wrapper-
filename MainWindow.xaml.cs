using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Microsoft.Web.WebView2.Core;

namespace YtDlpWrapper
{
    /// <summary>
    /// Represents a video format available for download from yt-dlp.
    /// </summary>
    public class YtDlpFormat
    {
        [JsonPropertyName("format_id")]
        public string FormatId { get; set; } = "";

        [JsonPropertyName("ext")]
        public string Extension { get; set; } = "";

        [JsonPropertyName("resolution")]
        public string Resolution { get; set; } = "";

        [JsonPropertyName("format_note")]
        public string FormatNote { get; set; } = "";

        [JsonPropertyName("filesize")]
        public long? Filesize { get; set; }
        
        [JsonPropertyName("vcodec")]
        public string Vcodec { get; set; } = "none";

        [JsonPropertyName("acodec")]
        public string Acodec { get; set; } = "none";
        
        [JsonPropertyName("height")]
        public int? Height { get; set; }

        public bool HasVideo => Vcodec != "none";
        public bool HasAudio => Acodec != "none";
    }

    /// <summary>
    /// Contains video information and available formats from yt-dlp.
    /// </summary>
    public class YtDlpVideoInfo
    {
        [JsonPropertyName("formats")]
        public List<YtDlpFormat> Formats { get; set; } = new();
    }

    /// <summary>
    /// Main window for the YT-DLP Wrapper application.
    /// </summary>
    public partial class MainWindow : Window
    {
        #region Constants
        private const string YtDlpExecutableName = "yt-dlp.exe";
        private const string FfmpegExecutableName = "ffmpeg.exe";
        private const string ApplicationName = "YT-DLP Wrapper";
        private const string OutputFileNameTemplate = "%(title)s.%(ext)s";
        private const string BlankPageUri = "about:blank";
        private const string YouTubeEmbedUrlTemplate = "https://www.youtube.com/embed/{0}?autoplay=0&controls=1&modestbranding=1&rel=0";
        private const int LogUpdateIntervalMs = 200;
        private const int VideoLoadDelayMs = 100;
        #endregion

        #region Private Fields
        private readonly string _tempDirectory = string.Empty;
        private readonly string _downloadsDirectory = string.Empty;
        private bool _isFirstVideoLoad = true;
        private List<YtDlpFormat> _availableFormats = new();
        private readonly ConcurrentQueue<string> _ytDlpLogQueue = new();
        private readonly DispatcherTimer _logUpdateTimer = new();
        private string _currentVideoUrl = string.Empty;
        private double _downloadProgress = 0;
        private readonly DispatcherTimer _progressAnimationTimer = new();
        private double _targetProgress = 0;
        #endregion
        
        /// <summary>
        /// Initializes a new instance of the MainWindow class.
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();
            
            // Initialize directories
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var appDirectory = Path.Combine(appDataPath, ApplicationName);
            _tempDirectory = Path.Combine(appDirectory, "temp");
            _downloadsDirectory = Path.Combine(appDirectory, "downloads");
            Directory.CreateDirectory(_tempDirectory);
            Directory.CreateDirectory(_downloadsDirectory);
            
            InitializeWebView();
            InitializeLogTimer();
            PopulateQualitySelector();
            
            LogMessage($"{ApplicationName} started successfully!");
            LogMessage("Simple workflow: 1) Load video for preview → 2) Choose quality → 3) Download");
            LogMessage($"Downloads will be saved to: {_downloadsDirectory}");
        }

        /// <summary>
        /// Initializes the log update timer.
        /// </summary>
        private void InitializeLogTimer()
        {
            _logUpdateTimer.Interval = TimeSpan.FromMilliseconds(LogUpdateIntervalMs);
            _logUpdateTimer.Tick += LogUpdateTimer_Tick;
            
            // Initialize progress animation timer
            _progressAnimationTimer.Interval = TimeSpan.FromMilliseconds(50);
            _progressAnimationTimer.Tick += ProgressAnimationTimer_Tick;
        }
        
        /// <summary>
        /// Handles the progress animation timer tick for smooth progress bar updates.
        /// </summary>
        private void ProgressAnimationTimer_Tick(object? sender, EventArgs e)
        {
            if (Math.Abs(_downloadProgress - _targetProgress) < 0.5)
            {
                _downloadProgress = _targetProgress;
                _progressAnimationTimer.Stop();
            }
            else
            {
                _downloadProgress += (_targetProgress - _downloadProgress) * 0.1;
            }
            
            Dispatcher.Invoke(() =>
            {
                ProgressBar.Value = _downloadProgress;
                ProgressPercentage.Text = $"{_downloadProgress:F1}%";
            });
        }

        /// <summary>
        /// Initializes the WebView2 component for YouTube video previews.
        /// </summary>
        private async void InitializeWebView()
        {
            try
            {
                await YouTubePlayer.EnsureCoreWebView2Async();
                LogMessage("🎬 WebView2 initialized successfully!");
            }
            catch (Exception ex)
            {
                LogMessage($"⚠️ WebView2 initialization failed: {ex.Message}");
                LogMessage("Video previews will not be available, but downloads will still work.");
            }
        }

        /// <summary>
        /// Logs a message to the output window with timestamp.
        /// </summary>
        /// <param name="message">The message to log.</param>
        private void LogMessage(string message)
        {
            if (string.IsNullOrEmpty(message)) return;
            
            Dispatcher.Invoke(() =>
            {
                var formattedMessage = $"[{DateTime.Now:HH:mm:ss}] {message}\n";
                LogOutput.AppendText(formattedMessage);
                LogOutput.ScrollToEnd();
            });
        }

        /// <summary>
        /// Handles the log update timer tick event to flush queued log messages.
        /// </summary>
        private void LogUpdateTimer_Tick(object? sender, EventArgs e)
        {
            if (_ytDlpLogQueue.IsEmpty) return;

            var sb = new StringBuilder();
            while (_ytDlpLogQueue.TryDequeue(out var line))
            {
                sb.AppendLine(line);
            }
            
            LogOutput.AppendText(sb.ToString());
            LogOutput.ScrollToEnd();
        }

        /// <summary>
        /// Determines if the provided URL is a YouTube video URL.
        /// </summary>
        /// <param name="url">The URL to check.</param>
        /// <returns>True if the URL is a YouTube video URL, false otherwise.</returns>
        private bool IsYouTubeUrl(string url)
        {
            if (string.IsNullOrWhiteSpace(url)) return false;
            
            return url.Contains("youtube.com/watch") || url.Contains("youtu.be/") || url.Contains("youtube.com/embed/");
        }

        /// <summary>
        /// Extracts the YouTube video ID from various YouTube URL formats.
        /// </summary>
        /// <param name="url">The YouTube URL.</param>
        /// <returns>The video ID if found, null otherwise.</returns>
        private string? ExtractYouTubeVideoId(string url)
        {
            if (string.IsNullOrWhiteSpace(url)) return null;
            
            // Handle different YouTube URL formats
            var patterns = new[]
            {
                @"(?:youtube\.com\/watch\?v=|youtu\.be\/|youtube\.com\/embed\/)([a-zA-Z0-9_-]{11})",
                @"youtube\.com\/watch\?.*?v=([a-zA-Z0-9_-]{11})"
            };

            foreach (var pattern in patterns)
            {
                var match = Regex.Match(url, pattern);
                if (match.Success)
                {
                    return match.Groups[1].Value;
                }
            }

            return null;
        }

        /// <summary>
        /// Loads a YouTube video in the embedded player.
        /// </summary>
        /// <param name="url">The YouTube video URL.</param>
        private async void LoadYouTubeVideo(string url)
        {
            try
            {
                var videoId = ExtractYouTubeVideoId(url);
                if (string.IsNullOrEmpty(videoId))
                {
                    LogMessage("❌ Could not extract video ID from YouTube URL");
                    return;
                }

                LogMessage($"🔄 Loading YouTube video: {videoId}");
                
                // Clear previous video if not the first load
                if (!_isFirstVideoLoad)
                {
                    await ClearPreviousVideo();
                }
                else
                {
                    _isFirstVideoLoad = false;
                }
                
                var embedUrl = string.Format(YouTubeEmbedUrlTemplate, videoId);
                YouTubePlayer.Source = new Uri(embedUrl);
                
                VideoPreviewSection.Visibility = Visibility.Visible;
                LogMessage("✅ YouTube video loaded successfully!");
            }
            catch (Exception ex)
            {
                LogMessage($"❌ Error loading YouTube video: {ex.Message}");
            }
        }

        /// <summary>
        /// Clears the previous video from the player.
        /// </summary>
        private async Task ClearPreviousVideo()
        {
            try
            {
                if (YouTubePlayer.Source != null)
                {
                    YouTubePlayer.Source = new Uri(BlankPageUri);
                    await Task.Delay(VideoLoadDelayMs);
                    LogMessage("🧹 Previous video cleared");
                }
            }
            catch (Exception ex)
            {
                LogMessage($"Note: Could not clear previous video: {ex.Message}");
            }
        }

        /// <summary>
        /// Handles the YouTube player navigation completed event.
        /// </summary>
        private void YouTubePlayer_NavigationCompleted(object sender, CoreWebView2NavigationCompletedEventArgs e)
        {
            if (e.IsSuccess)
            {
                if (_isFirstVideoLoad)
                {
                    LogMessage("🎬 YouTube preview ready!");
                    _isFirstVideoLoad = false;
                }
            }
            else
            {
                LogMessage($"Failed to load video in WebView2: {e.WebErrorStatus}");
            }
        }

        /// <summary>
        /// Fetches available video formats from yt-dlp for the specified URL.
        /// </summary>
        /// <param name="url">The video URL to analyze.</param>
        private async Task FetchAvailableFormatsAsync(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                LogMessage("❌ Invalid URL provided for format analysis.");
                return;
            }

            LogMessage("🔍 Analyzing available video formats...");
            LogMessage($"🔄 Fetching fresh format data for: {url}");
            
            // Always clear formats before fetching new ones
            _availableFormats.Clear();
            
            var processInfo = new ProcessStartInfo
            {
                FileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, YtDlpExecutableName),
                Arguments = $"--dump-json --no-cache-dir \"{url}\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                WorkingDirectory = AppDomain.CurrentDomain.BaseDirectory
            };

            using var process = new Process { StartInfo = processInfo };
            var jsonOutput = new StringBuilder();
            var errorOutput = new StringBuilder();
            
            process.OutputDataReceived += (s, args) => 
            {
                if (!string.IsNullOrEmpty(args.Data)) 
                    jsonOutput.Append(args.Data);
            };
            
            process.ErrorDataReceived += (s, args) => 
            {
                if (!string.IsNullOrEmpty(args.Data)) 
                    errorOutput.AppendLine(args.Data);
            };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            await Task.Run(() => process.WaitForExit());

            if (process.ExitCode == 0)
            {
                try
                {
                    var videoInfo = JsonSerializer.Deserialize<YtDlpVideoInfo>(jsonOutput.ToString());
                    if (videoInfo?.Formats != null)
                    {
                        _availableFormats = videoInfo.Formats;
                        LogMessage($"✅ Found {_availableFormats.Count} available formats.");
                        
                        // Log available qualities for debugging
                        var availableQualities = _availableFormats
                            .Where(f => f.Height.HasValue)
                            .Select(f => f.Height!.Value)
                            .Distinct()
                            .OrderByDescending(h => h)
                            .ToList();
                        
                        if (availableQualities.Any())
                        {
                            LogMessage($"📊 Available qualities: {string.Join(", ", availableQualities)}p");
                        }
                    }
                    else
                    {
                        LogMessage("⚠️ No formats found in response.");
                        _availableFormats = new List<YtDlpFormat>();
                    }
                }
                catch (JsonException ex)
                {
                    LogMessage($"❌ Failed to parse format information: {ex.Message}");
                    _availableFormats = new List<YtDlpFormat>();
                }
            }
            else
            {
                LogMessage($"❌ Could not fetch formats. Error: {errorOutput}");
                _availableFormats = new List<YtDlpFormat>();
            }
        }

        /// <summary>
        /// Populates the quality selector with available video formats.
        /// </summary>
        private void PopulateQualitySelector()
        {
            Dispatcher.Invoke(() =>
            {
                QualitySelector.Items.Clear();
                LogMessage("🔄 Populating quality selector with fresh format data...");

                // Handle case where format detection fails
                if (_availableFormats == null || !_availableFormats.Any())
                {
                    LogMessage("⚠️ No formats found or failed to fetch. Using fallback quality options.");
                    AddFallbackQualityOptions();
                    return;
                }

                // Create a dictionary of quality labels and their corresponding resolution values
                var qualityLevels = new Dictionary<string, int>
                {
                    { "📺 4K (2160p)", 2160 }, { "🎬 1440p (2K)", 1440 },
                    { "📹 1080p (FHD)", 1080 }, { "🎥 720p (HD)", 720 },
                    { "📱 480p", 480 }, { "💾 360p", 360 }
                };

                // Find the maximum available video height
                var videoFormats = _availableFormats.Where(f => f.Height.HasValue).ToList();
                if (videoFormats.Any())
                {
                    var maxHeight = videoFormats.Max(f => f.Height!.Value);
                    LogMessage($"📊 Maximum available quality: {maxHeight}p");
                    
                    // Add quality options that are available for the current video
                    var addedCount = 0;
                    foreach (var level in qualityLevels.Where(l => l.Value <= maxHeight))
                    {
                        QualitySelector.Items.Add(new ComboBoxItem { Content = level.Key, Tag = level.Value.ToString() });
                        addedCount++;
                    }
                    
                    LogMessage($"📝 Added {addedCount} quality options to selector");
                }
                else
                {
                    LogMessage("⚠️ No video formats with height information found");
                }

                // Add audio-only option
                QualitySelector.Items.Add(new ComboBoxItem { Content = "--- Audio Only ---", IsEnabled = false });
                QualitySelector.Items.Add(new ComboBoxItem { Content = "🎵 Best Audio (m4a)", Tag = "bestaudio[ext=m4a]" });
                
                QualitySelector.SelectedIndex = 0; // Default to the highest available quality
                LogMessage($"✅ Quality selector populated with {QualitySelector.Items.Count} total options");
            });
        }
        
        /// <summary>
        /// Adds fallback quality options when format detection fails.
        /// </summary>
        private void AddFallbackQualityOptions()
        {
            var fallbackOptions = new Dictionary<string, string>
            {
                { "🔥 Best Available", "best" }, 
                { "📹 1080p (FHD)", "1080" },
                { "🎥 720p (HD)", "720" }, 
                { "📱 480p", "480" },
                { "🎵 Best Audio (m4a)", "bestaudio[ext=m4a]" }
            };
            
            foreach (var option in fallbackOptions)
            {
                QualitySelector.Items.Add(new ComboBoxItem { Content = option.Key, Tag = option.Value });
            }
            
            QualitySelector.SelectedIndex = 0;
        }

        /// <summary>
        /// Handles the load button click event to load a video for preview and format detection.
        /// </summary>
        private async void LoadButton_Click(object sender, RoutedEventArgs e)
        {
            var url = UrlInput.Text.Trim();
            if (string.IsNullOrEmpty(url))
            {
                MessageBox.Show("Please enter a video URL.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            LoadButton.IsEnabled = false;
            LoadButton.Content = "🔄 Loading...";

            try
            {
                // Always clear previous state when loading a video
                ClearVideoState();
                _currentVideoUrl = url;
                
                if (IsYouTubeUrl(url))
                {
                    LogMessage($"🎬 Loading YouTube video for preview: {url}");
                    LoadYouTubeVideo(url);
                }
                else
                {
                    LogMessage($"🔗 Non-YouTube URL detected: {url}");
                    VideoPreviewSection.Visibility = Visibility.Collapsed;
                }
                
                // Fetch formats and then populate the UI
                await FetchAvailableFormatsAsync(url);
                PopulateQualitySelector();
                
                QualitySelectionPanel.Visibility = Visibility.Visible;
                LogMessage("✅ Ready to download! Choose your preferred quality.");
            }
            catch (Exception ex)
            {
                LogMessage($"❌ Error loading video: {ex.Message}");
                MessageBox.Show($"Error loading video: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                LoadButton.IsEnabled = true;
                LoadButton.Content = "🎬 Load Video";
            }
        }

        /// <summary>
        /// Clears the video state before loading a new video.
        /// </summary>
        private void ClearVideoState()
        {
            _availableFormats.Clear();
            QualitySelector.Items.Clear();
            LogMessage("🔄 Clearing previous video state...");
        }

        /// <summary>
        /// Handles the download button click event to start video download.
        /// </summary>
        private async void DownloadButton_Click(object sender, RoutedEventArgs e)
        {
            var url = UrlInput.Text.Trim();
            if (string.IsNullOrEmpty(url))
            {
                MessageBox.Show("Please load a video first.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            DownloadButton.IsEnabled = false;
            ShowProgressBar(true);
            ResetProgress();

            try
            {
                var selectedQuality = (QualitySelector.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "720p (HD)";
                var selectedTag = (QualitySelector.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "720";
                
                LogMessage($"🐱 Starting download for: {url}");
                LogMessage($"📊 Selected quality: {selectedQuality}");
                LogMessage($"🔧 Quality tag: {selectedTag}");
                
                await DownloadVideoAsync(url);
            }
            catch (Exception ex)
            {
                LogMessage($"Error during download: {ex.Message}");
                MessageBox.Show($"Download failed: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                DownloadButton.IsEnabled = true;
                ShowProgressBar(false);
            }
        }
        
        /// <summary>
        /// Shows or hides the progress bar and related UI elements.
        /// </summary>
        /// <param name="show">Whether to show the progress bar.</param>
        private void ShowProgressBar(bool show)
        {
            var visibility = show ? Visibility.Visible : Visibility.Collapsed;
            ProgressBar.Visibility = visibility;
            ProgressLabel.Visibility = visibility;
            ProgressStatus.Visibility = visibility;
            ProgressSpeed.Visibility = visibility;
            ProgressPercentage.Visibility = visibility;
            
            if (!show)
            {
                ProgressBar.IsIndeterminate = false;
                _progressAnimationTimer.Stop();
            }
        }
        
        /// <summary>
        /// Resets the progress bar to initial state.
        /// </summary>
        private void ResetProgress()
        {
            _downloadProgress = 0;
            _targetProgress = 0;
            ProgressBar.Value = 0;
            ProgressBar.IsIndeterminate = false;
            ProgressStatus.Text = "Initializing download...";
            ProgressSpeed.Text = "";
            ProgressPercentage.Text = "0%";
        }

        /// <summary>
        /// Downloads a video using yt-dlp with the selected quality format.
        /// </summary>
        /// <param name="url">The video URL to download.</param>
        private async Task DownloadVideoAsync(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                LogMessage("❌ Invalid URL provided for download.");
                return;
            }

            var outputPath = Path.Combine(_downloadsDirectory, OutputFileNameTemplate);
            var selectedItem = QualitySelector.SelectedItem as ComboBoxItem;

            if (selectedItem?.Tag == null)
            {
                LogMessage("❌ No quality format selected.");
                return;
            }

            var selectionTag = selectedItem.Tag?.ToString() ?? "720";
            var formatArgument = BuildFormatArgument(selectionTag);
            
            LogMessage($"✅ Using robust format selector: {formatArgument}");

            var arguments = new List<string>
            {
                "--format", $"\"{formatArgument}\"",
                "--merge-output-format", "mp4",
                "--no-playlist",
                "--no-cache-dir",
                "--force-overwrites",
                "--output", $"\"{outputPath}\"",
                $"\"{url}\""
            };
            
            var argumentString = string.Join(" ", arguments);
            
            var processInfo = new ProcessStartInfo
            {
                FileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, YtDlpExecutableName),
                Arguments = argumentString,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                WorkingDirectory = AppDomain.CurrentDomain.BaseDirectory
            };

            LogMessage($"Executing yt-dlp: yt-dlp {argumentString}");
            _logUpdateTimer.Start();
            
            // Initialize progress to 0 and wait for real progress
            UpdateProgress(0, "🐱 Initializing download...", "");

            using var process = new Process { StartInfo = processInfo };
            
            process.OutputDataReceived += (s, args) =>
            {
                if (!string.IsNullOrEmpty(args.Data))
                {
                    _ytDlpLogQueue.Enqueue($"yt-dlp: {args.Data}");
                    ParseDownloadProgress(args.Data);
                }
            };
            
            process.ErrorDataReceived += (s, args) =>
            {
                if (!string.IsNullOrEmpty(args.Data)) 
                    _ytDlpLogQueue.Enqueue($"yt-dlp Error: {args.Data}");
            };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            await Task.Run(() => process.WaitForExit());
            _logUpdateTimer.Stop();
            LogUpdateTimer_Tick(null, EventArgs.Empty); // Final flush of logs

            if (process.ExitCode == 0)
            {
                // Complete the progress to 100% only when actually finished
                UpdateProgress(100, "🐱 Download completed!", "");
                await Task.Delay(1000); // Show 100% for a moment
                
                LogMessage("✅ Download completed successfully!");
                var result = MessageBox.Show("🐱 Download complete! Open downloads folder?", "Success", MessageBoxButton.YesNo, MessageBoxImage.Information);
                if (result == MessageBoxResult.Yes)
                {
                    OpenDownloadsFolder();
                }
            }
            else
            {
                UpdateProgress(_targetProgress, "🐱 Download failed", "");
                throw new Exception($"yt-dlp failed with exit code {process.ExitCode}");
            }
        }

        /// <summary>
        /// Builds the format argument string for yt-dlp based on the selected quality.
        /// </summary>
        /// <param name="selectionTag">The quality selection tag.</param>
        /// <returns>The format argument string.</returns>
        private string BuildFormatArgument(string selectionTag)
        {
            if (string.IsNullOrEmpty(selectionTag))
            {
                LogMessage("⚠️ No quality tag provided, using 'best' format");
                return "best";
            }
            
            if (selectionTag.Contains("bestaudio"))
            {
                LogMessage($"🎵 Using audio-only format: {selectionTag}");
                return selectionTag; // Audio-only selection
            }
            
            // Handle resolution-based selection
            var formatArgument = $"bestvideo[height<={selectionTag}][ext=mp4]+bestaudio[ext=m4a]/best[height<={selectionTag}][ext=mp4]/best";
            LogMessage($"📹 Using video format for {selectionTag}p: {formatArgument}");
            return formatArgument;
        }
        
        /// <summary>
        /// Parses download progress from yt-dlp output.
        /// </summary>
        /// <param name="output">The yt-dlp output line.</param>
        private void ParseDownloadProgress(string output)
        {
            try
            {
                // More comprehensive regex patterns for different yt-dlp output formats
                var progressPatterns = new[]
                {
                    @"(\d+(?:\.\d+)?)%\s+of\s+[\d.]+\w+\s+at\s+([\d.]+\w+/s)(?:\s+ETA\s+([\d:]+))?",  // Standard format
                    @"\[(\d+(?:\.\d+)?)%\]\s+([\d.]+\w+/s)?",  // Alternative format
                    @"(\d+(?:\.\d+)?)%.*?([\d.]+\w+/s).*?ETA\s+([\d:]+)",  // With ETA
                    @"(\d+(?:\.\d+)?)%"  // Just percentage
                };
                
                double? percentage = null;
                string? speed = null;
                string? eta = null;
                
                foreach (var pattern in progressPatterns)
                {
                    var match = Regex.Match(output, pattern);
                    if (match.Success)
                    {
                        if (double.TryParse(match.Groups[1].Value, out var parsedPercentage))
                        {
                            percentage = parsedPercentage;
                        }
                        
                        if (match.Groups.Count > 2 && !string.IsNullOrEmpty(match.Groups[2].Value))
                        {
                            speed = match.Groups[2].Value;
                        }
                        
                        if (match.Groups.Count > 3 && !string.IsNullOrEmpty(match.Groups[3].Value))
                        {
                            eta = match.Groups[3].Value;
                        }
                        
                        break;
                    }
                }
                
                // Update progress if we found a percentage
                if (percentage.HasValue)
                {
                    var currentPhase = DetermineDownloadPhase(output);
                    var adjustedPercentage = AdjustProgressForPhase(percentage.Value, currentPhase);
                    
                    UpdateProgress(adjustedPercentage, currentPhase.StatusMessage, speed ?? "");
                    
                    if (!string.IsNullOrEmpty(eta))
                    {
                        Dispatcher.Invoke(() => ProgressStatus.Text = $"{currentPhase.StatusMessage} ETA: {eta}");
                    }
                }
                else
                {
                    // Check for phase changes without explicit percentage
                    var phase = DetermineDownloadPhase(output);
                    if (phase.StatusMessage != "🐱 Downloading...")
                    {
                        Dispatcher.Invoke(() => ProgressStatus.Text = phase.StatusMessage);
                        
                        // Only update progress if we're in a post-download phase
                        if (phase.MinProgress > _targetProgress)
                        {
                            UpdateProgress(phase.MinProgress, phase.StatusMessage, speed ?? "");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogMessage($"Error parsing progress: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Determines the current download phase based on output.
        /// </summary>
        /// <param name="output">The yt-dlp output line.</param>
        /// <returns>Download phase information.</returns>
        private (string StatusMessage, double MinProgress) DetermineDownloadPhase(string output)
        {
            var lowerOutput = output.ToLowerInvariant();
            
            if (lowerOutput.Contains("downloading") && lowerOutput.Contains("video"))
                return ("🐱 Downloading video...", 0);
            
            if (lowerOutput.Contains("downloading") && lowerOutput.Contains("audio"))
                return ("🐱 Downloading audio...", 0);
            
            if (lowerOutput.Contains("merging") || lowerOutput.Contains("muxing"))
                return ("🐱 Merging video and audio...", 85);
            
            if (lowerOutput.Contains("converting") || lowerOutput.Contains("post-processing"))
                return ("🐱 Converting format...", 90);
            
            if (lowerOutput.Contains("finalizing") || lowerOutput.Contains("moving"))
                return ("🐱 Finalizing...", 95);
            
            if (lowerOutput.Contains("downloading"))
                return ("🐱 Downloading...", 0);
            
            return ("🐱 Processing...", 0);
        }
        
        /// <summary>
        /// Adjusts progress percentage based on the current phase.
        /// </summary>
        /// <param name="rawPercentage">The raw percentage from yt-dlp.</param>
        /// <param name="phase">The current download phase.</param>
        /// <returns>Adjusted percentage.</returns>
        private double AdjustProgressForPhase(double rawPercentage, (string StatusMessage, double MinProgress) phase)
        {
            // For video/audio downloading phases, use the raw percentage
            if (phase.StatusMessage.Contains("Downloading"))
            {
                return rawPercentage;
            }
            
            // For post-processing phases, ensure we don't go backwards
            return Math.Max(rawPercentage, phase.MinProgress);
        }
        
        /// <summary>
        /// Updates the progress bar with smooth animation.
        /// </summary>
        /// <param name="percentage">The progress percentage.</param>
        /// <param name="status">The status message.</param>
        /// <param name="speed">The download speed.</param>
        private void UpdateProgress(double percentage, string status, string speed)
        {
            _targetProgress = Math.Max(0, Math.Min(100, percentage));
            
            Dispatcher.Invoke(() =>
            {
                if (!string.IsNullOrEmpty(status))
                    ProgressStatus.Text = status;
                if (!string.IsNullOrEmpty(speed))
                    ProgressSpeed.Text = speed;
            });
            
            if (!_progressAnimationTimer.IsEnabled)
            {
                _progressAnimationTimer.Start();
            }
        }



        /// <summary>
        /// Handles the open downloads button click event.
        /// </summary>
        private void OpenDownloadsButton_Click(object sender, RoutedEventArgs e)
        {
            OpenDownloadsFolder();
        }

        /// <summary>
        /// Opens the downloads folder in Windows Explorer.
        /// </summary>
        private void OpenDownloadsFolder()
        {
            try
            {
                Process.Start("explorer.exe", _downloadsDirectory);
                LogMessage($"Opened downloads folder: {_downloadsDirectory}");
            }
            catch (Exception ex)
            {
                LogMessage($"Could not open downloads folder: {ex.Message}");
                MessageBox.Show($"Could not open downloads folder: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Handles the clear video button click event to reset the application state.
        /// </summary>
        private async void ClearVideoButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Clear the URL input
                UrlInput.Text = string.Empty;
                _currentVideoUrl = string.Empty;
                
                // Hide quality selection panel
                QualitySelectionPanel.Visibility = Visibility.Collapsed;
                
                // Clear available formats
                _availableFormats.Clear();
                QualitySelector.Items.Clear();
                
                // Clear YouTube player and hide preview section
                await ClearYouTubePlayer();
                
                // Reset first load flag for next video
                _isFirstVideoLoad = true;
                
                // Clear log output and restore startup messages
                ResetLogOutput();
                
                LogMessage("✅ Session cleared. Ready for new video.");
            }
            catch (Exception ex)
            {
                LogMessage($"Error clearing video: {ex.Message}");
            }
        }

        /// <summary>
        /// Clears the YouTube player and hides the preview section.
        /// </summary>
        private async Task ClearYouTubePlayer()
        {
            try
            {
                YouTubePlayer.Source = new Uri(BlankPageUri);
                await Task.Delay(VideoLoadDelayMs);
                VideoPreviewSection.Visibility = Visibility.Collapsed;
            }
            catch (Exception)
            {
                // Ignore clearing errors on reset
            }
        }

        /// <summary>
        /// Resets the log output to show startup messages.
        /// </summary>
        private void ResetLogOutput()
        {
            LogOutput.Text = string.Empty;
            LogMessage($"{ApplicationName} ready!");
            LogMessage("Simple workflow: 1) Load video for preview → 2) Choose quality → 3) Download");
            LogMessage($"Downloads will be saved to: {_downloadsDirectory}");
        }

        /// <summary>
        /// Handles the clear temporary files button click event.
        /// </summary>
        private void ClearTempButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var result = MessageBox.Show("This will delete all temporary files. Continue?", 
                                           "Clear Temporary Files", 
                                           MessageBoxButton.YesNo, 
                                           MessageBoxImage.Question);
                
                if (result == MessageBoxResult.Yes)
                {
                    var deletedCount = ClearTemporaryFiles();
                    
                    LogMessage($"✅ Cleared {deletedCount} temporary files.");
                    
                    if (deletedCount > 0)
                    {
                        MessageBox.Show($"Successfully deleted {deletedCount} temporary files.", 
                                      "Cleanup Complete", 
                                      MessageBoxButton.OK, 
                                      MessageBoxImage.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                LogMessage($"Error clearing temp files: {ex.Message}");
                MessageBox.Show($"Error clearing temp files: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Clears all temporary files from the temp directory.
        /// </summary>
        /// <returns>The number of files successfully deleted.</returns>
        private int ClearTemporaryFiles()
        {
            var files = Directory.GetFiles(_tempDirectory);
            int deletedCount = 0;
            
            foreach (var file in files)
            {
                try
                {
                    File.Delete(file);
                    deletedCount++;
                }
                catch (Exception ex)
                {
                    LogMessage($"Could not delete {Path.GetFileName(file)}: {ex.Message}");
                }
            }
            
            return deletedCount;
        }





        #region Window Control Event Handlers
        
        /// <summary>
        /// Handles the title bar mouse left button down event for dragging and maximizing.
        /// </summary>
        private void TitleBar_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                MaximizeButton_Click(sender, new RoutedEventArgs());
            }
            else
            {
                this.DragMove();
            }
        }

        /// <summary>
        /// Handles the minimize button click event.
        /// </summary>
        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        /// <summary>
        /// Handles the maximize button click event to toggle between normal and maximized states.
        /// </summary>
        private void MaximizeButton_Click(object sender, RoutedEventArgs? e)
        {
            if (this.WindowState == WindowState.Normal)
            {
                this.WindowState = WindowState.Maximized;
                MaximizeButton.Content = "🗗";
            }
            else
            {
                this.WindowState = WindowState.Normal;
                MaximizeButton.Content = "🗖";
            }
        }

        /// <summary>
        /// Handles the close button click event.
        /// </summary>
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        /// <summary>
        /// Handles the window closed event to clean up resources.
        /// </summary>
        protected override void OnClosed(EventArgs e)
        {
            try
            {
                // Stop any playing video before closing
                if (YouTubePlayer?.Source != null)
                {
                    YouTubePlayer.Source = new Uri(BlankPageUri);
                }
                YouTubePlayer?.Dispose();
            }
            catch { /* Ignore disposal errors */ }
            
            base.OnClosed(e);
        }
        
        #endregion



        #region Window Resize Functionality

        /// <summary>
        /// Handles the resize border mouse enter event.
        /// </summary>
        private void ResizeBorder_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (sender is Border border)
            {
                border.Background = System.Windows.Media.Brushes.Transparent;
            }
        }

        /// <summary>
        /// Handles the resize border mouse leave event.
        /// </summary>
        private void ResizeBorder_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (sender is Border border)
            {
                border.Background = System.Windows.Media.Brushes.Transparent;
            }
        }

        /// <summary>
        /// Handles the resize border mouse left button down event.
        /// </summary>
        private void ResizeBorder_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (sender is not Border border) return;

            // Determine resize direction based on border name
            var direction = border.Name switch
            {
                "ResizeBorderTop" => ResizeDirection.Top,
                "ResizeBorderBottom" => ResizeDirection.Bottom,
                "ResizeBorderLeft" => ResizeDirection.Left,
                "ResizeBorderRight" => ResizeDirection.Right,
                _ => (ResizeDirection?)null
            };
            
            if (direction.HasValue)
            {
                DragResizeWindow(direction.Value);
            }
        }

        /// <summary>
        /// Handles the resize grip mouse left button down event.
        /// </summary>
        private void ResizeGrip_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (sender is not Border border) return;

            // Determine resize direction based on grip name
            var direction = border.Name switch
            {
                "ResizeGripTopLeft" => ResizeDirection.TopLeft,
                "ResizeGripTopRight" => ResizeDirection.TopRight,
                "ResizeGripBottomLeft" => ResizeDirection.BottomLeft,
                "ResizeGripBottomRight" => ResizeDirection.BottomRight,
                _ => (ResizeDirection?)null
            };
            
            if (direction.HasValue)
            {
                DragResizeWindow(direction.Value);
            }
        }

        /// <summary>
        /// Initiates window resize dragging in the specified direction.
        /// </summary>
        /// <param name="direction">The resize direction.</param>
        private void DragResizeWindow(ResizeDirection direction)
        {
            try
            {
                var hwnd = new System.Windows.Interop.WindowInteropHelper(this).Handle;
                var wParam = (IntPtr)(0xF000 | (int)direction);
                SendMessage(hwnd, 0x112, wParam, IntPtr.Zero);
            }
            catch (Exception ex)
            {
                LogMessage($"Resize error: {ex.Message}");
            }
        }

        /// <summary>
        /// Sends a message to the specified window.
        /// </summary>
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr SendMessage(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

        /// <summary>
        /// Enumeration for window resize directions.
        /// </summary>
        private enum ResizeDirection
        {
            Left = 1,
            Right = 2,
            Top = 3,
            TopLeft = 4,
            TopRight = 5,
            Bottom = 6,
            BottomLeft = 7,
            BottomRight = 8,
        }

        #endregion
    }
} 