using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.Windows;
using System.IO;
using System.Linq;
using YtDlpWrapper.Models;
using YtDlpWrapper.Services;

namespace YtDlpWrapper.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        private readonly YtDlpService _ytDlpService;
        private readonly VideoProcessingService _videoProcessingService;

        [ObservableProperty]
        private DownloadInfo _downloadInfo = new()
        {
            DownloadDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "YtDlpWrapper", "Downloads")
        };

        [ObservableProperty]
        private VideoInfo _currentVideo = new();

        [ObservableProperty]
        private CropRange _cropRange = new();

        [ObservableProperty]
        private string _logOutput = string.Empty;

        [ObservableProperty]
        private bool _isDownloading;

        [ObservableProperty]
        private bool _isProcessing;

        [ObservableProperty]
        private string _selectedVideoPath = string.Empty;

        public ObservableCollection<string> DownloadedVideos { get; } = new();

        public MainViewModel()
        {
            _ytDlpService = new YtDlpService();
            _videoProcessingService = new VideoProcessingService();

            // Subscribe to events
            _ytDlpService.LogOutput += OnLogOutput;
            _ytDlpService.ProgressChanged += OnProgressChanged;
            _ytDlpService.DownloadCompleted += OnDownloadCompleted;

            _videoProcessingService.LogOutput += OnLogOutput;
            _videoProcessingService.ProcessingCompleted += OnProcessingCompleted;

            LoadDownloadedVideos();
        }

        [RelayCommand]
        private async Task DownloadAndLoadAsync()
        {
            if (string.IsNullOrWhiteSpace(DownloadInfo.Url))
            {
                MessageBox.Show("Please enter a valid URL.", "Invalid URL", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            IsDownloading = true;
            DownloadInfo.IsDownloading = true;
            DownloadInfo.Status = "Starting download...";
            DownloadInfo.Progress = 0;
            DownloadInfo.ProgressText = "0%";

            try
            {
                await _ytDlpService.StartDownloadAsync(DownloadInfo);
            }
            catch (Exception ex)
            {
                LogOutput += $"Error: {ex.Message}\n";
                DownloadInfo.Status = "Download failed";
            }
        }

        [RelayCommand]
        private void CancelDownload()
        {
            _ytDlpService.CancelDownload();
            IsDownloading = false;
            DownloadInfo.IsDownloading = false;
            DownloadInfo.Status = "Download cancelled";
        }

        [RelayCommand]
        private void BrowseDownloadDirectory()
        {
            // Use a simple input dialog for directory selection
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Title = "Select Download Directory",
                ValidateNames = false,
                CheckFileExists = false,
                CheckPathExists = true,
                FileName = "Select Folder"
            };

            if (dialog.ShowDialog() == true)
            {
                var directory = System.IO.Path.GetDirectoryName(dialog.FileName);
                if (!string.IsNullOrEmpty(directory))
                {
                    DownloadInfo.DownloadDirectory = directory;
                }
            }
        }

        [RelayCommand]
        private void ClearLog()
        {
            LogOutput = string.Empty;
        }

        [RelayCommand]
        private void PasteUrl()
        {
            if (Clipboard.ContainsText())
            {
                DownloadInfo.Url = Clipboard.GetText();
            }
        }

        [RelayCommand]
        private void ClearUrl()
        {
            DownloadInfo.Url = string.Empty;
        }

        [RelayCommand]
        private async Task LoadVideoAsync()
        {
            var dialog = new OpenFileDialog
            {
                Title = "Select Video File",
                Filter = "Video Files (*.mp4;*.webm;*.mkv;*.avi)|*.mp4;*.webm;*.mkv;*.avi|All Files (*.*)|*.*"
            };

            if (dialog.ShowDialog() == true)
            {
                await LoadVideoFromPathAsync(dialog.FileName);
            }
        }

        [RelayCommand]
        private async Task CropVideoAsync()
        {
            if (string.IsNullOrEmpty(CurrentVideo.FilePath))
            {
                MessageBox.Show("Please load a video first.", "No Video", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!CropRange.IsValid)
            {
                MessageBox.Show("Please set a valid crop range (start time must be less than end time).", "Invalid Range", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            IsProcessing = true;
            try
            {
                // Create cropped file in the same directory as original
                var originalPath = CurrentVideo.FilePath;
                var originalName = System.IO.Path.GetFileNameWithoutExtension(originalPath);
                var croppedPath = System.IO.Path.Combine(
                    System.IO.Path.GetDirectoryName(originalPath)!,
                    $"{originalName}_cropped.mp4"
                );

                LogOutput += $"Cropping video...\n";
                await _videoProcessingService.CropVideoAsync(originalPath, croppedPath, CropRange);
                
                // Delete original file
                if (System.IO.File.Exists(croppedPath))
                {
                    System.IO.File.Delete(originalPath);
                    LogOutput += $"Original file deleted. Cropped file saved as: {System.IO.Path.GetFileName(croppedPath)}\n";
                    
                    // Load the cropped video
                    await LoadVideoFromPathAsync(croppedPath);
                }
            }
            catch (Exception ex)
            {
                LogOutput += $"Cropping error: {ex.Message}\n";
                MessageBox.Show($"Error cropping video: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsProcessing = false;
            }
        }

        private async Task LoadVideoFromPathAsync(string filePath)
        {
            System.Diagnostics.Debug.WriteLine($"LoadVideoFromPathAsync called with: {filePath}");
            
            try
            {
                SelectedVideoPath = filePath;
                CurrentVideo.FilePath = filePath;
                CurrentVideo.Title = System.IO.Path.GetFileNameWithoutExtension(filePath);

                System.Diagnostics.Debug.WriteLine($"CurrentVideo.FilePath set to: {CurrentVideo.FilePath}");
                System.Diagnostics.Debug.WriteLine($"CurrentVideo.Title set to: {CurrentVideo.Title}");

                // Force property change notification to update UI
                OnPropertyChanged(nameof(CurrentVideo));
                OnPropertyChanged(nameof(SelectedVideoPath));
                
                System.Diagnostics.Debug.WriteLine("Property change notifications sent");

                // Get video duration (optional - won't fail if FFmpeg is missing)
                try
                {
                    var duration = await _videoProcessingService.GetVideoDurationAsync(filePath);
                    if (duration.HasValue)
                    {
                        CurrentVideo.TotalDuration = duration.Value;
                        CurrentVideo.Duration = duration.Value.ToString(@"hh\:mm\:ss");
                        
                        // Set crop range to full video
                        CropRange.SetRange(TimeSpan.Zero, duration.Value);
                    }
                    else
                    {
                        // If we can't get duration, set a default and let user know
                        CurrentVideo.TotalDuration = TimeSpan.Zero;
                        CurrentVideo.Duration = "Unknown";
                        LogOutput += "Note: Video duration unknown (FFmpeg not available). Video will still play.\n";
                    }
                }
                catch (Exception ex)
                {
                    LogOutput += $"Warning: Could not get video duration: {ex.Message}\n";
                    CurrentVideo.TotalDuration = TimeSpan.Zero;
                    CurrentVideo.Duration = "Unknown";
                }

                // Add to downloaded videos list if not already present
                if (!DownloadedVideos.Contains(filePath))
                {
                    DownloadedVideos.Add(filePath);
                }

                LogOutput += $"Video loaded: {System.IO.Path.GetFileName(filePath)}\n";
                
                System.Diagnostics.Debug.WriteLine("About to force final UI update");
                
                // Force UI update
                Application.Current.Dispatcher.Invoke(() =>
                {
                    OnPropertyChanged(nameof(CurrentVideo));
                    System.Diagnostics.Debug.WriteLine("Final OnPropertyChanged(CurrentVideo) called from dispatcher");
                });
            }
            catch (Exception ex)
            {
                LogOutput += $"Error loading video: {ex.Message}\n";
            }
        }

        private void OnLogOutput(object? sender, string message)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                LogOutput += $"{DateTime.Now:HH:mm:ss} - {message}\n";
            });
        }

        private void OnProgressChanged(object? sender, Services.DownloadProgressEventArgs e)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                DownloadInfo.Progress = e.Progress;
                DownloadInfo.ProgressText = $"{e.Progress:F1}%";
                DownloadInfo.Speed = e.Speed;
            });
        }

        private async void OnDownloadCompleted(object? sender, Services.DownloadCompletedEventArgs e)
        {
            await Application.Current.Dispatcher.InvokeAsync(async () =>
            {
                IsDownloading = false;
                DownloadInfo.IsDownloading = false;

                if (e.Success && !string.IsNullOrEmpty(e.FilePath))
                {
                    DownloadInfo.Status = "Download completed successfully";
                    DownloadInfo.Progress = 100;
                    DownloadInfo.ProgressText = "100%";
                    
                    // Move file from temp to final directory and load it
                    var movedFile = await MoveDownloadedFileAsync(e.FilePath!);
                    if (!string.IsNullOrEmpty(movedFile))
                    {
                        await LoadVideoFromPathAsync(movedFile);
                    }
                }
                else
                {
                    DownloadInfo.Status = "Download failed";
                }
            });
        }

        private async Task<string?> MoveDownloadedFileAsync(string sourceFile)
        {
            try
            {
                if (!System.IO.File.Exists(sourceFile))
                    return null;
                var fileName = System.IO.Path.GetFileName(sourceFile);
                var destinationFile = System.IO.Path.Combine(DownloadInfo.DownloadDirectory, fileName);
                // Ensure unique filename
                var counter = 1;
                while (System.IO.File.Exists(destinationFile))
                {
                    var nameWithoutExt = System.IO.Path.GetFileNameWithoutExtension(fileName);
                    var ext = System.IO.Path.GetExtension(fileName);
                    destinationFile = System.IO.Path.Combine(DownloadInfo.DownloadDirectory, $"{nameWithoutExt}_{counter}{ext}");
                    counter++;
                }
                System.IO.File.Move(sourceFile, destinationFile);
                LogOutput += $"File moved to: {destinationFile}\n";
                return destinationFile;
            }
            catch (Exception ex)
            {
                LogOutput += $"Error moving file: {ex.Message}\n";
                return null;
            }
        }

        private void OnProcessingCompleted(object? sender, bool success)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                IsProcessing = false;
                if (success)
                {
                    LogOutput += "Video processing completed successfully.\n";
                }
                else
                {
                    LogOutput += "Video processing failed.\n";
                }
            });
        }

        private void LoadDownloadedVideos()
        {
            try
            {
                var downloadDir = DownloadInfo.DownloadDirectory;
                if (Directory.Exists(downloadDir))
                {
                    var videoFiles = Directory.GetFiles(downloadDir, "*.mp4")
                        .Concat(Directory.GetFiles(downloadDir, "*.webm"))
                        .Concat(Directory.GetFiles(downloadDir, "*.mkv"))
                        .Concat(Directory.GetFiles(downloadDir, "*.avi"));

                    foreach (var file in videoFiles)
                    {
                        DownloadedVideos.Add(file);
                    }
                }
            }
            catch (Exception ex)
            {
                LogOutput += $"Error loading downloaded videos: {ex.Message}\n";
            }
        }

        [RelayCommand]
        private void OpenVideoFolder()
        {
            try
            {
                var folderPath = DownloadInfo.DownloadDirectory;
                if (Directory.Exists(folderPath))
                {
                    System.Diagnostics.Process.Start("explorer.exe", folderPath);
                }
                else
                {
                    MessageBox.Show($"Folder does not exist: {folderPath}", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error opening folder: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        [RelayCommand]
        private void SetCropStart()
        {
            if (CurrentVideo.TotalDuration > TimeSpan.Zero)
            {
                CropRange.StartTime = CurrentVideo.CurrentPosition;
                LogOutput += $"Crop start time set to: {CropRange.FormattedStartTime} (from video position: {CurrentVideo.CurrentPosition})\n";
                System.Diagnostics.Debug.WriteLine($"[Crop] Start time set to {CropRange.StartTime}. IsValid: {CropRange.IsValid}");
            }
        }

        [RelayCommand]
        private void SetCropEnd()
        {
            if (CurrentVideo.TotalDuration > TimeSpan.Zero)
            {
                CropRange.EndTime = CurrentVideo.CurrentPosition;
                LogOutput += $"Crop end time set to: {CropRange.FormattedEndTime} (from video position: {CurrentVideo.CurrentPosition})\n";
                System.Diagnostics.Debug.WriteLine($"[Crop] End time set to {CropRange.EndTime}. IsValid: {CropRange.IsValid}");
            }
        }

        public void Dispose()
        {
            _ytDlpService?.Dispose();
            _videoProcessingService?.Dispose();
        }
    }
} 