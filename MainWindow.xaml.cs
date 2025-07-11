using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Microsoft.Web.WebView2.Core;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace YtDlpWrapper
{
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

    public class YtDlpVideoInfo
    {
        [JsonPropertyName("formats")]
        public List<YtDlpFormat> Formats { get; set; } = new();
    }

    public partial class MainWindow : Window
    {
        private readonly string _tempDirectory;
        private readonly string _downloadsDirectory;
        private bool _isFirstVideoLoad = true;
        private List<YtDlpFormat> _availableFormats = new();

        public MainWindow()
        {
            InitializeComponent();
            InitializeWebView();
            
            // Set up directories
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var appDirectory = Path.Combine(appDataPath, "YT-DLP Wrapper");
            _tempDirectory = Path.Combine(appDirectory, "temp");
            _downloadsDirectory = Path.Combine(appDirectory, "downloads");
            Directory.CreateDirectory(_tempDirectory);
            Directory.CreateDirectory(_downloadsDirectory);

            // Populate quality selector on startup
            PopulateQualitySelector();
            
            LogMessage("YT-DLP Wrapper started successfully!");
            LogMessage("Simple workflow: 1) Load video for preview → 2) Choose quality → 3) Download");
            LogMessage($"Downloads will be saved to: {_downloadsDirectory}");
        }

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

        private void LogMessage(string message)
        {
            Dispatcher.Invoke(() =>
            {
                var timestamp = DateTime.Now.ToString("HH:mm:ss");
                LogOutput.Text += $"[{timestamp}] {message}\n";
                LogOutput.ScrollToEnd();
            });
        }

        private bool IsYouTubeUrl(string url)
        {
            return url.Contains("youtube.com/watch") || url.Contains("youtu.be/") || url.Contains("youtube.com/embed/");
        }

        private string? ExtractYouTubeVideoId(string url)
        {
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

        private async void LoadYouTubeVideo(string url)
        {
            try
            {
                var videoId = ExtractYouTubeVideoId(url);
                if (!string.IsNullOrEmpty(videoId))
                {
                    LogMessage($"🔄 Loading YouTube video: {videoId}");
                    
                    // Only try to clear if this isn't the first video load
                    if (!_isFirstVideoLoad)
                    {
                        try
                        {
                            if (YouTubePlayer.Source != null)
                            {
                                // Navigate to blank page to stop all media playback
                                YouTubePlayer.Source = new Uri("about:blank");
                                await Task.Delay(100); // Brief pause for cleanup
                                LogMessage("🧹 Previous video cleared");
                            }
                        }
                        catch (Exception clearEx)
                        {
                            LogMessage($"Note: Could not clear previous video: {clearEx.Message}");
                        }
                    }
                    else
                    {
                        _isFirstVideoLoad = false; // Mark that we've loaded our first video
                    }
                    
                    var embedUrl = $"https://www.youtube.com/embed/{videoId}?autoplay=0&controls=1&modestbranding=1&rel=0";
                    YouTubePlayer.Source = new Uri(embedUrl);
                    
                    // Show the video preview section
                    VideoPreviewSection.Visibility = Visibility.Visible;
                    LogMessage($"✅ YouTube video loaded successfully!");
                }
                else
                {
                    LogMessage("❌ Could not extract video ID from YouTube URL");
                }
            }
            catch (Exception ex)
            {
                LogMessage($"❌ Error loading YouTube video: {ex.Message}");
            }
        }

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

        private async Task FetchAvailableFormatsAsync(string url)
        {
            LogMessage("🔍 Analyzing available video formats...");
            var processInfo = new ProcessStartInfo
            {
                FileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "yt-dlp.exe"),
                Arguments = $"--dump-json \"{url}\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                WorkingDirectory = AppDomain.CurrentDomain.BaseDirectory
            };

            using (var process = new Process { StartInfo = processInfo })
            {
                var jsonOutput = "";
                process.OutputDataReceived += (s, args) => { if (!string.IsNullOrEmpty(args.Data)) jsonOutput += args.Data; };
                var errorOutput = "";
                process.ErrorDataReceived += (s, args) => { if (!string.IsNullOrEmpty(args.Data)) errorOutput += args.Data; };

                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                await Task.Run(() => process.WaitForExit());

                if (process.ExitCode == 0)
                {
                    var videoInfo = JsonSerializer.Deserialize<YtDlpVideoInfo>(jsonOutput);
                    if (videoInfo?.Formats != null)
                    {
                        _availableFormats = videoInfo.Formats;
                        LogMessage($"✅ Found {_availableFormats.Count} available formats.");
                    }
                }
                else
                {
                    LogMessage($"❌ Could not fetch formats. Error: {errorOutput}");
                    _availableFormats = new List<YtDlpFormat>(); // Ensure list is empty on failure
                }
            }
        }

        private void PopulateQualitySelector()
        {
            Dispatcher.Invoke(() =>
            {
                QualitySelector.Items.Clear();

                // Handle case where format detection fails
                if (_availableFormats == null || !_availableFormats.Any())
                {
                    LogMessage("⚠️ No formats found or failed to fetch. Using fallback quality options.");
                    // Provide a generic, robust list if detection fails
                    var fallbackOptions = new Dictionary<string, string>
                    {
                        { "🔥 Best Available", "best" }, { "📹 1080p (FHD)", "1080" },
                        { "🎥 720p (HD)", "720" }, { "📱 480p", "480" },
                        { "🎵 Best Audio (m4a)", "bestaudio[ext=m4a]" }
                    };
                    foreach (var option in fallbackOptions)
                    {
                        QualitySelector.Items.Add(new ComboBoxItem { Content = option.Key, Tag = option.Value });
                    }
                    QualitySelector.SelectedIndex = 0;
                    return;
                }

                // Create a dictionary of quality labels and their corresponding resolution values
                var qualityLevels = new Dictionary<string, int>
                {
                    { "🔥 Best Available", 9999 }, // Always show "Best Available"
                    { "📺 4K (2160p)", 2160 }, { "🎬 1440p (2K)", 1440 },
                    { "📹 1080p (FHD)", 1080 }, { "🎥 720p (HD)", 720 },
                    { "📱 480p", 480 }, { "💾 360p", 360 }
                };

                // Find the maximum available video height
                var maxHeight = _availableFormats
                    .Where(f => f.Height.HasValue)
                    .Max(f => f.Height.Value);

                // Add quality options that are available for the current video
                foreach (var level in qualityLevels)
                {
                    if (level.Value == 9999 || level.Value <= maxHeight)
                    {
                        var tagValue = level.Value == 9999 ? "best" : level.Value.ToString();
                        QualitySelector.Items.Add(new ComboBoxItem { Content = level.Key, Tag = tagValue });
                    }
                }

                // Add audio-only option
                QualitySelector.Items.Add(new ComboBoxItem { Content = "--- Audio Only ---", IsEnabled = false });
                QualitySelector.Items.Add(new ComboBoxItem { Content = "🎵 Best Audio (m4a)", Tag = "bestaudio[ext=m4a]" });
                
                QualitySelector.SelectedIndex = 0; // Default to Best Available
                LogMessage("✅ Dynamic quality options populated.");
            });
        }
        
        private void AddFallbackQualityOptions()
        {
            var defaultItems = new[]
            {
                "🔥 Best Available", "📹 1080p (FHD)", "🎥 720p (HD)", "📱 480p", "--- Audio Only ---", "🎵 Best Quality Audio (m4a)"
            };
            foreach (var item in defaultItems)
            {
                var comboBoxItem = new ComboBoxItem { Content = item, Tag = item };
                if (item.StartsWith("---")) comboBoxItem.IsEnabled = false;
                QualitySelector.Items.Add(comboBoxItem);
            }
            QualitySelector.SelectedIndex = 0;
        }

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

        private async void DownloadButton_Click(object sender, RoutedEventArgs e)
        {
            var url = UrlInput.Text.Trim();
            if (string.IsNullOrEmpty(url))
            {
                MessageBox.Show("Please load a video first.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            DownloadButton.IsEnabled = false;
            ProgressBar.Visibility = Visibility.Visible;
            ProgressBar.IsIndeterminate = true;

            try
            {
                var selectedQuality = ((ComboBoxItem)QualitySelector.SelectedItem)?.Content?.ToString() ?? "720p (HD)";
                LogMessage($"Starting download for: {url}");
                LogMessage($"Selected quality: {selectedQuality}");
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
                ProgressBar.Visibility = Visibility.Collapsed;
                ProgressBar.IsIndeterminate = false;
            }
        }

        private async Task DownloadVideoAsync(string url)
        {
            var outputPath = Path.Combine(_downloadsDirectory, "%(title)s.%(ext)s");
            var selectedItem = QualitySelector.SelectedItem as ComboBoxItem;

            if (selectedItem?.Tag == null)
            {
                LogMessage("❌ No quality format selected.");
                return;
            }

            var selectionTag = selectedItem.Tag.ToString();
            string formatArgument;

            if (selectionTag.Contains("bestaudio"))
            {
                // Handle audio-only selection
                formatArgument = selectionTag;
            }
            else if (selectionTag == "best")
            {
                // Handle "Best Available" selection
                formatArgument = "bestvideo[ext=mp4]+bestaudio[ext=m4a]/best[ext=mp4]/best";
            }
            else
            {
                // Handle resolution-based selection
                formatArgument = $"bestvideo[height<={selectionTag}][ext=mp4]+bestaudio[ext=m4a]/best[height<={selectionTag}][ext=mp4]/best";
            }
            
            LogMessage($"✅ Using robust format selector: {formatArgument}");

            var arguments = new List<string>
            {
                "--format", $"\"{formatArgument}\"",
                "--merge-output-format", "mp4",
                "--no-playlist",
                "--output", $"\"{outputPath}\"",
                $"\"{url}\""
            };
            
            var argumentString = string.Join(" ", arguments);
            
            var processInfo = new ProcessStartInfo
            {
                FileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "yt-dlp.exe"),
                Arguments = argumentString,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                WorkingDirectory = AppDomain.CurrentDomain.BaseDirectory
            };

            LogMessage($"Executing yt-dlp: yt-dlp {argumentString}");

            using (var process = new Process { StartInfo = processInfo })
            {
                process.OutputDataReceived += (s, args) =>
                {
                    if (!string.IsNullOrEmpty(args.Data)) LogMessage($"yt-dlp: {args.Data}");
                };
                process.ErrorDataReceived += (s, args) =>
                {
                    if (!string.IsNullOrEmpty(args.Data)) LogMessage($"yt-dlp Error: {args.Data}");
                };

                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                await Task.Run(() => process.WaitForExit());

                if (process.ExitCode == 0)
                {
                    LogMessage("✅ Download completed successfully!");
                    var result = MessageBox.Show("Download complete! Open downloads folder?", "Success", MessageBoxButton.YesNo, MessageBoxImage.Information);
                    if (result == MessageBoxResult.Yes)
                    {
                        OpenDownloadsFolder();
                    }
                }
                else
                {
                    throw new Exception($"yt-dlp failed with exit code {process.ExitCode}");
                }
            }
        }



        private void OpenDownloadsButton_Click(object sender, RoutedEventArgs e)
        {
            OpenDownloadsFolder();
        }

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

        private async void ClearVideoButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Clear the URL input
                UrlInput.Text = "";
                
                // Hide quality selection panel
                QualitySelectionPanel.Visibility = Visibility.Collapsed;
                
                // Clear available formats
                _availableFormats.Clear();
                QualitySelector.Items.Clear();
                
                // Clear YouTube player and hide preview section
                try
                {
                    // Navigate to blank page to stop all media playback
                    YouTubePlayer.Source = new Uri("about:blank");
                    await Task.Delay(100); // Small delay to ensure navigation completes
                    
                    // Hide the preview section
                    VideoPreviewSection.Visibility = Visibility.Collapsed;
                }
                catch (Exception)
                {
                    // Ignore clearing errors on reset
                }
                
                // Reset first load flag for next video
                _isFirstVideoLoad = true;
                
                // Clear log output (keep only startup messages)
                LogOutput.Text = "";
                LogMessage("YT-DLP Wrapper ready!");
                LogMessage("Simple workflow: 1) Load video for preview → 2) Choose quality → 3) Download");
                LogMessage($"Downloads will be saved to: {_downloadsDirectory}");
                
                LogMessage("✅ Session cleared. Ready for new video.");
            }
            catch (Exception ex)
            {
                LogMessage($"Error clearing video: {ex.Message}");
            }
        }

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





        // Window Control Event Handlers
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

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

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

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        protected override void OnClosed(EventArgs e)
        {
            try
            {
                // Stop any playing video before closing
                if (YouTubePlayer?.Source != null)
                {
                    YouTubePlayer.Source = new Uri("about:blank");
                }
                YouTubePlayer?.Dispose();
            }
            catch { /* Ignore disposal errors */ }
            
            base.OnClosed(e);
        }



        #region Window Resize Functionality

        private void ResizeBorder_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            var border = sender as Border;
            if (border != null)
            {
                border.Background = System.Windows.Media.Brushes.Transparent;
            }
        }

        private void ResizeBorder_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            var border = sender as Border;
            if (border != null)
            {
                border.Background = System.Windows.Media.Brushes.Transparent;
            }
        }

        private void ResizeBorder_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var border = sender as Border;
            if (border == null) return;

            // Determine resize direction based on border name
            if (border.Name == "ResizeBorderTop")
                DragResizeWindow(ResizeDirection.Top);
            else if (border.Name == "ResizeBorderBottom")
                DragResizeWindow(ResizeDirection.Bottom);
            else if (border.Name == "ResizeBorderLeft")
                DragResizeWindow(ResizeDirection.Left);
            else if (border.Name == "ResizeBorderRight")
                DragResizeWindow(ResizeDirection.Right);
        }

        private void ResizeGrip_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var border = sender as Border;
            if (border == null) return;

            // Determine resize direction based on grip name
            if (border.Name == "ResizeGripTopLeft")
                DragResizeWindow(ResizeDirection.TopLeft);
            else if (border.Name == "ResizeGripTopRight")
                DragResizeWindow(ResizeDirection.TopRight);
            else if (border.Name == "ResizeGripBottomLeft")
                DragResizeWindow(ResizeDirection.BottomLeft);
            else if (border.Name == "ResizeGripBottomRight")
                DragResizeWindow(ResizeDirection.BottomRight);
        }

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

        [System.Runtime.InteropServices.DllImport("user32.dll", CharSet = System.Runtime.InteropServices.CharSet.Auto)]
        private static extern IntPtr SendMessage(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

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