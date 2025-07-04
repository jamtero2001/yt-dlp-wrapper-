using System;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Microsoft.Web.WebView2.Core;

namespace YtDlpWrapper
{
    public partial class MainWindow : Window
    {
        private readonly string _tempDirectory;
        private readonly string _downloadsDirectory;
        private bool _isFirstVideoLoad = true;

        public MainWindow()
        {
            InitializeComponent();

            // Set up directories
            _tempDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "temp");
            _downloadsDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "downloads");
            
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

        private string GetQualityFormat()
        {
            string selectedQuality = "";
            Dispatcher.Invoke(() =>
            {
                var selectedItem = QualitySelector.SelectedItem as ComboBoxItem;
                selectedQuality = selectedItem?.Content?.ToString() ?? "🎥 720p (HD)";
            });

            // Map quality selection to yt-dlp format strings with proper video+audio merging
            // Using format strings that prefer AAC audio codec for better compatibility
            return selectedQuality switch
            {
                "🔥 Best Available" => "bv*[ext=mp4]+ba[ext=m4a]/bv*+ba/b[ext=mp4]/best",
                "📺 2160p (4K)" => "bv[height<=2160]+ba/bv[height<=2160][ext=mp4]+ba[ext=m4a]/b[height<=2160][ext=mp4]",
                "🎬 1440p (2K)" => "bv[height<=1440]+ba/bv[height<=1440][ext=mp4]+ba[ext=m4a]/b[height<=1440][ext=mp4]", 
                "📹 1080p (FHD)" => "bv[height<=1080]+ba/bv[height<=1080][ext=mp4]+ba[ext=m4a]/b[height<=1080][ext=mp4]",
                "🎥 720p (HD)" => "bv[height<=720]+ba/bv[height<=720][ext=mp4]+ba[ext=m4a]/b[height<=720][ext=mp4]",
                "📱 480p" => "bv[height<=480]+ba/bv[height<=480][ext=mp4]+ba[ext=m4a]/b[height<=480][ext=mp4]",
                "💾 360p" => "bv[height<=360]+ba/bv[height<=360][ext=mp4]+ba[ext=m4a]/b[height<=360][ext=mp4]",
                "📞 240p" => "bv[height<=240]+ba/bv[height<=240][ext=mp4]+ba[ext=m4a]/b[height<=240][ext=mp4]",
                "⚡ 144p" => "bv[height<=144]+ba/bv[height<=144][ext=mp4]+ba[ext=m4a]/b[height<=144][ext=mp4]",
                _ => "bv[height<=720]+ba/bv[height<=720][ext=mp4]+ba[ext=m4a]/b[height<=720][ext=mp4]" // Default fallback
            };
        }

        private void LoadButton_Click(object sender, RoutedEventArgs e)
        {
            var url = UrlInput.Text.Trim();
            if (string.IsNullOrEmpty(url))
            {
                MessageBox.Show("Please enter a video URL.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                LoadButton.IsEnabled = false;
                LoadButton.Content = "🔄 Loading...";
                
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

                // Show quality selection panel for download
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
            var qualityFormat = GetQualityFormat();
            
            var processInfo = new ProcessStartInfo
            {
                FileName = "yt-dlp.exe",
                Arguments = $"--format \"{qualityFormat}\" --merge-output-format mp4 --audio-format aac --prefer-free-formats --output \"{outputPath}\" \"{url}\"",
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
    }
} 