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

namespace YtDlpWrapper
{
    public class VideoFormat
    {
        public string Id { get; set; } = "";
        public string Extension { get; set; } = "";
        public string Resolution { get; set; } = "";
        public string Note { get; set; } = "";
        public string FileSize { get; set; } = "";
        public string DisplayName { get; set; } = "";
        public bool HasVideo { get; set; }
        public bool HasAudio { get; set; }
        public int Height { get; set; }
        public string Codec { get; set; } = "";
    }

    public partial class MainWindow : Window
    {
        private readonly string _tempDirectory;
        private readonly string _downloadsDirectory;
        private bool _isFirstVideoLoad = true;
        private List<VideoFormat> _availableFormats = new List<VideoFormat>();

        public MainWindow()
        {
            InitializeComponent();

            // Set up directories in user-writable locations
            var userDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "YT-DLP Wrapper");
            _tempDirectory = Path.Combine(userDataPath, "temp");
            _downloadsDirectory = Path.Combine(userDataPath, "downloads");
            
            Directory.CreateDirectory(_tempDirectory);
            Directory.CreateDirectory(_downloadsDirectory);

            // Initialize WebView2
            InitializeWebView();

            LogMessage("YT-DLP Wrapper started successfully!");
            LogMessage("Simple workflow: 1) Load video for preview → 2) Choose quality → 3) Download");
            LogMessage($"Downloads will be saved to: {_downloadsDirectory}");
        }

        private async void InitializeWebView()
        {
            try
            {
                await YouTubePlayer.EnsureCoreWebView2Async();
                LogMessage("YouTube player initialized successfully.");
            }
            catch (Exception ex)
            {
                LogMessage($"Failed to initialize YouTube player: {ex.Message}");
                LogMessage("WebView2 runtime may not be installed. Download and install WebView2 Runtime from Microsoft.");
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
                                YouTubePlayer.Source = null;
                                await Task.Delay(50); // Brief pause for cleanup
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

        private async Task<List<VideoFormat>> FetchAvailableFormatsAsync(string url)
        {
            var formats = new List<VideoFormat>();
            
            try
            {
                LogMessage("🔍 Analyzing available video formats...");
                
                var processInfo = new ProcessStartInfo
                {
                    FileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "yt-dlp.exe"),
                    Arguments = $"-F \"{url}\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    WorkingDirectory = AppDomain.CurrentDomain.BaseDirectory
                };

                using (var process = new Process { StartInfo = processInfo })
                {
                    var output = "";
                    process.OutputDataReceived += (s, args) =>
                    {
                        if (!string.IsNullOrEmpty(args.Data))
                        {
                            output += args.Data + "\n";
                        }
                    };

                    process.Start();
                    process.BeginOutputReadLine();
                    await Task.Run(() => process.WaitForExit());

                    if (process.ExitCode == 0)
                    {
                        formats = ParseFormatsFromOutput(output);
                        LogMessage($"✅ Found {formats.Count} available formats");
                    }
                    else
                    {
                        LogMessage("❌ Could not fetch available formats");
                    }
                }
            }
            catch (Exception ex)
            {
                LogMessage($"Error fetching formats: {ex.Message}");
            }
            
            return formats;
        }

        private List<VideoFormat> ParseFormatsFromOutput(string output)
        {
            var formats = new List<VideoFormat>();
            var lines = output.Split('\n');
            bool foundFormatSection = false;

            foreach (var line in lines)
            {
                // Look for the actual header line
                if (line.Contains("ID") && line.Contains("EXT") && line.Contains("RESOLUTION"))
                {
                    foundFormatSection = true;
                    continue;
                }

                // Skip separator lines and empty lines
                if (!foundFormatSection || string.IsNullOrWhiteSpace(line) || line.Contains("─"))
                    continue;

                // Skip storyboard formats
                if (line.Contains("storyboard") || line.Contains("mhtml"))
                    continue;

                try
                {
                    // Parse the new yt-dlp format
                    // Format: ID EXT RESOLUTION FPS CH │ FILESIZE TBR PROTO │ VCODEC VBR ACODEC ABR ASR MORE INFO
                    var parts = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length < 3) continue;

                    var format = new VideoFormat
                    {
                        Id = parts[0],
                        Extension = parts[1],
                        Resolution = parts[2]
                    };

                    // Find filesize (look for patterns like "1.23MiB", "456KiB")
                    var fileSizeMatch = Regex.Match(line, @"(\d+\.?\d*[KMG]iB)");
                    if (fileSizeMatch.Success)
                    {
                        format.FileSize = fileSizeMatch.Groups[1].Value;
                    }

                    // Determine format type based on content
                    var lowerLine = line.ToLower();
                    format.HasVideo = !lowerLine.Contains("audio only");
                    format.HasAudio = !lowerLine.Contains("video only");

                    // Extract height from resolution (240p, 360p, 720p, 1080p, etc.)
                    var heightMatch = Regex.Match(format.Resolution, @"(\d+)x(\d+)");
                    if (heightMatch.Success)
                    {
                        int height;
                        if (int.TryParse(heightMatch.Groups[2].Value, out height))
                        {
                            format.Height = height;
                        }
                    }

                    // Extract codec information
                    if (lowerLine.Contains("avc1") || lowerLine.Contains("h264"))
                        format.Codec += "H.264 ";
                    if (lowerLine.Contains("vp9"))
                        format.Codec += "VP9 ";
                    if (lowerLine.Contains("av01"))
                        format.Codec += "AV1 ";
                    if (lowerLine.Contains("mp4a") || lowerLine.Contains("aac"))
                        format.Codec += "AAC ";
                    if (lowerLine.Contains("opus"))
                        format.Codec += "Opus ";

                    // Get additional info from the line
                    var moreInfoStart = line.LastIndexOf(']');
                    if (moreInfoStart > 0 && moreInfoStart < line.Length - 1)
                    {
                        format.Note = line.Substring(moreInfoStart + 1).Trim();
                    }

                    // Create display name with better formatting
                    if (format.HasVideo && format.HasAudio)
                    {
                        var resolution = $"{format.Height}p" ?? format.Resolution;
                        var size = !string.IsNullOrEmpty(format.FileSize) ? $" ({format.FileSize})" : "";
                        var codec = !string.IsNullOrEmpty(format.Codec) ? $" - {format.Codec.Trim()}" : "";
                        format.DisplayName = $"📺 {resolution}{codec}{size}";
                    }
                    else if (format.HasVideo && !format.HasAudio)
                    {
                        var resolution = $"{format.Height}p" ?? format.Resolution;
                        var codec = !string.IsNullOrEmpty(format.Codec) ? $" - {format.Codec.Trim()}" : "";
                        format.DisplayName = $"🎬 {resolution} Video Only{codec}";
                    }
                    else if (!format.HasVideo && format.HasAudio)
                    {
                        var size = !string.IsNullOrEmpty(format.FileSize) ? $" ({format.FileSize})" : "";
                        var codec = !string.IsNullOrEmpty(format.Codec) ? $" - {format.Codec.Trim()}" : "";
                        format.DisplayName = $"🎵 Audio Only{codec}{size}";
                    }
                    else
                    {
                        continue; // Skip unknown formats
                    }

                    formats.Add(format);
                }
                catch (Exception ex)
                {
                    // Skip problematic lines but continue parsing
                    LogMessage($"Debug: Skipped line parsing: {ex.Message}");
                    continue;
                }
            }

            // Filter and sort formats intelligently
            var result = new List<VideoFormat>();

            // Find the best AAC audio format for combining with video-only formats
            var bestAacAudio = formats
                .Where(f => !f.HasVideo && f.HasAudio && f.Codec.Contains("AAC"))
                .OrderByDescending(f => f.Id == "140") // Prefer format 140 (standard AAC)
                .FirstOrDefault();

            // Add native combined video+audio formats first
            var nativeCombinedFormats = formats
                .Where(f => f.HasVideo && f.HasAudio)
                .OrderByDescending(f => f.Height)
                .ThenBy(f => f.Extension == "mp4" ? 0 : 1) // Prefer MP4
                .ThenBy(f => f.Codec.Contains("AAC") ? 0 : 1) // Prefer AAC audio
                .ThenBy(f => f.Codec.Contains("Opus") ? 1 : 0) // Deprioritize Opus
                .ToList();

            result.AddRange(nativeCombinedFormats);

            // Create synthetic combined formats by pairing video-only with best audio
            if (bestAacAudio != null)
            {
                var videoFormatsForCombining = formats
                    .Where(f => f.HasVideo && !f.HasAudio && f.Height >= 144)
                    .Where(f => f.Extension == "mp4" && f.Codec.Contains("H.264")) // Prefer H.264 MP4
                    .GroupBy(f => f.Height)
                    .OrderByDescending(g => g.Key)
                    .Take(8) // Get top 8 resolutions
                    .ToList();

                foreach (var group in videoFormatsForCombining)
                {
                    var videoFormat = group.First();
                    var syntheticFormat = new VideoFormat
                    {
                        Id = $"{videoFormat.Id}+{bestAacAudio.Id}",
                        Extension = "mp4",
                        Resolution = videoFormat.Resolution,
                        Height = videoFormat.Height,
                        HasVideo = true,
                        HasAudio = true,
                        Codec = $"{videoFormat.Codec.Trim()} + AAC",
                        FileSize = "Auto-Combined",
                        DisplayName = $"📺 {videoFormat.Height}p - H.264 + AAC (Auto-Combined)"
                    };
                    result.Add(syntheticFormat);
                }
            }

            // Add video-only formats for advanced users
            var videoOnlyFormats = formats
                .Where(f => f.HasVideo && !f.HasAudio && f.Height >= 720)
                .OrderByDescending(f => f.Height)
                .ThenBy(f => f.Extension == "mp4" ? 0 : 1)
                .Take(5)
                .ToList();

            result.AddRange(videoOnlyFormats);

            // Add a few audio-only formats (prefer AAC over Opus)
            var audioFormats = formats
                .Where(f => !f.HasVideo && f.HasAudio)
                .OrderBy(f => f.Codec.Contains("AAC") ? 0 : 1) // Prefer AAC
                .ThenBy(f => f.Extension == "m4a" ? 0 : 1) // Prefer M4A
                .Take(3)
                .ToList();

            result.AddRange(audioFormats);

            return result;
        }

        private string GetSelectedFormatId()
        {
            string selectedFormatId = "best";
            Dispatcher.Invoke(() =>
            {
                var selectedItem = QualitySelector.SelectedItem as ComboBoxItem;
                if (selectedItem?.Tag is VideoFormat format)
                {
                    selectedFormatId = format.Id;
                }
            });
            return selectedFormatId;
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
                // Show the YouTube player if it's a YouTube URL
                if (IsYouTubeUrl(url))
                {
                    LogMessage($"🎬 Loading YouTube video for preview: {url}");
                    LoadYouTubeVideo(url);
                }
                else
                {
                    LogMessage($"🔗 Non-YouTube URL detected: {url}");
                    LogMessage("Preview not available for this URL, but you can still download it.");
                }

                // Fetch available formats
                _availableFormats = await FetchAvailableFormatsAsync(url);
                
                // Populate quality selector
                await PopulateQualitySelector();
                
                // Show quality selection panel
                QualitySelectionPanel.Visibility = Visibility.Visible;
                LogMessage("✅ Ready to download! Choose your preferred quality and click 'Download Now'.");
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

        private async Task PopulateQualitySelector()
        {
            await Dispatcher.InvokeAsync(() =>
            {
                QualitySelector.Items.Clear();

                if (_availableFormats.Count == 0)
                {
                    // Fallback to default options if format detection failed
                    var defaultItems = new[]
                    {
                        "🔥 Best Available",
                        "📺 2160p (4K)",
                        "🎬 1440p (2K)",
                        "📹 1080p (FHD)",
                        "🎥 720p (HD)",
                        "📱 480p",
                        "💾 360p"
                    };

                    foreach (var item in defaultItems)
                    {
                        QualitySelector.Items.Add(new ComboBoxItem { Content = item });
                    }
                    QualitySelector.SelectedIndex = 4; // Default to 720p
                    LogMessage("⚠️ Using default quality options (format detection failed)");
                    return;
                }

                // Add "Best Available" option first
                var bestItem = new ComboBoxItem 
                { 
                    Content = "🔥 Best Available (Auto-Select)",
                    Tag = null // Special case for best
                };
                QualitySelector.Items.Add(bestItem);

                // Group formats by resolution for cleaner display
                var videoFormats = _availableFormats
                    .Where(f => f.HasVideo && f.HasAudio)
                    .GroupBy(f => f.Height)
                    .OrderByDescending(g => g.Key);

                foreach (var group in videoFormats)
                {
                    var bestFormat = group.OrderBy(f => f.Extension == "mp4" ? 0 : 1).First();
                    var item = new ComboBoxItem
                    {
                        Content = bestFormat.DisplayName,
                        Tag = bestFormat
                    };
                    QualitySelector.Items.Add(item);
                }

                // Add video-only formats if available
                var videoOnlyFormats = _availableFormats
                    .Where(f => f.HasVideo && !f.HasAudio && f.Height > 0)
                    .GroupBy(f => f.Height)
                    .OrderByDescending(g => g.Key)
                    .Take(3); // Limit to top 3 resolutions

                if (videoOnlyFormats.Any())
                {
                    QualitySelector.Items.Add(new ComboBoxItem { Content = "--- Video Only ---", IsEnabled = false });
                    foreach (var group in videoOnlyFormats)
                    {
                        var bestFormat = group.OrderBy(f => f.Extension == "mp4" ? 0 : 1).First();
                        var item = new ComboBoxItem
                        {
                            Content = bestFormat.DisplayName,
                            Tag = bestFormat
                        };
                        QualitySelector.Items.Add(item);
                    }
                }

                // Add audio-only formats
                var audioFormats = _availableFormats
                    .Where(f => !f.HasVideo && f.HasAudio)
                    .Take(3);

                if (audioFormats.Any())
                {
                    QualitySelector.Items.Add(new ComboBoxItem { Content = "--- Audio Only ---", IsEnabled = false });
                    foreach (var format in audioFormats)
                    {
                        var item = new ComboBoxItem
                        {
                            Content = format.DisplayName,
                            Tag = format
                        };
                        QualitySelector.Items.Add(item);
                    }
                }

                // Select best available by default
                QualitySelector.SelectedIndex = 0;
                LogMessage($"📋 Populated {QualitySelector.Items.Count} quality options");
            });
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
                LogMessage("📥 Downloading with MP4 video and AAC audio for maximum compatibility.");
                LogMessage("🎵 Audio format optimized to avoid Opus codec issues.");
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
            string formatArgument;
            
            // Get the selected format
            var selectedItem = QualitySelector.SelectedItem as ComboBoxItem;
            var selectedFormat = selectedItem?.Tag as VideoFormat;
            
            if (selectedFormat != null)
            {
                // Use specific format ID
                formatArgument = selectedFormat.Id;
                LogMessage($"📥 Using format ID: {selectedFormat.Id} ({selectedFormat.DisplayName})");
                
                // For video-only formats, manually combine with best AAC audio
                if (selectedFormat.HasVideo && !selectedFormat.HasAudio)
                {
                    formatArgument = $"{selectedFormat.Id}+140/best"; // 140 is typically AAC audio
                    LogMessage($"🎵 Combining video format {selectedFormat.Id} with AAC audio (140)");
                }
            }
            else
            {
                // Fallback to best quality for default options or "Best Available"
                var selectedText = selectedItem?.Content?.ToString() ?? "";
                formatArgument = selectedText switch
                {
                    var text when text.Contains("Best Available") => "best",
                    var text when text.Contains("2160p") || text.Contains("4K") => "bestvideo[height<=2160]+bestaudio/best[height<=2160]",
                    var text when text.Contains("1440p") || text.Contains("2K") => "bestvideo[height<=1440]+bestaudio/best[height<=1440]",
                    var text when text.Contains("1080p") => "bestvideo[height<=1080]+bestaudio/best[height<=1080]",
                    var text when text.Contains("720p") => "bestvideo[height<=720]+bestaudio/best[height<=720]",
                    var text when text.Contains("480p") => "bestvideo[height<=480]+bestaudio/best[height<=480]",
                    var text when text.Contains("360p") => "bestvideo[height<=360]+bestaudio/best[height<=360]",
                    _ => "best"
                };
                LogMessage($"📥 Using fallback format: {formatArgument}");
            }
            
            var processInfo = new ProcessStartInfo
            {
                FileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "yt-dlp.exe"),
                Arguments = $"--format \"{formatArgument}\" --merge-output-format mp4 --audio-quality 0 --output \"{outputPath}\" \"{url}\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                WorkingDirectory = AppDomain.CurrentDomain.BaseDirectory
            };

            LogMessage("Executing yt-dlp...");

            using (var process = new Process { StartInfo = processInfo })
            {
                process.OutputDataReceived += (s, args) =>
                {
                    if (!string.IsNullOrEmpty(args.Data))
                    {
                        LogMessage($"yt-dlp: {args.Data}");
                    }
                };

                process.ErrorDataReceived += (s, args) =>
                {
                    if (!string.IsNullOrEmpty(args.Data))
                    {
                        LogMessage($"yt-dlp Error: {args.Data}");
                    }
                };

                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                await Task.Run(() => process.WaitForExit());

                if (process.ExitCode == 0)
                {
                    LogMessage("✅ Download completed successfully!");
                    LogMessage("📁 Use the 'Show Downloads Folder' button below to locate your downloaded video.");
                    
                    // Show success message and ask if user wants to open folder
                    var result = MessageBox.Show("Video downloaded successfully!\n\nWould you like to open the downloads folder to view your video?", 
                                                "Download Complete", 
                                                MessageBoxButton.YesNo, 
                                                MessageBoxImage.Information);
                    
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

        private void YouTubePlayer_NavigationCompleted(object sender, CoreWebView2NavigationCompletedEventArgs e)
        {
            if (e.IsSuccess)
            {
                LogMessage("🎬 YouTube preview ready!");
            }
            else
            {
                LogMessage("❌ Failed to load YouTube preview.");
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

        private void ClearVideoButton_Click(object sender, RoutedEventArgs e)
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
                
                // Clear YouTube player
                try
                {
                    YouTubePlayer.Source = null;
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