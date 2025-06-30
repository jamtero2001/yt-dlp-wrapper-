using System.Diagnostics;
using System.Text.RegularExpressions;
using System.IO;
using System.Net.Http;
using Newtonsoft.Json;
using YtDlpWrapper.Models;
using System.Globalization;

namespace YtDlpWrapper.Services
{
    public class YtDlpService
    {
        private readonly string _ytDlpPath;
        private readonly string _ffmpegPath;
        private Process? _currentProcess;
        private readonly CancellationTokenSource _cancellationTokenSource = new();

        public event EventHandler<string>? LogOutput;
        public event EventHandler<DownloadProgressEventArgs>? ProgressChanged;
        public event EventHandler<DownloadCompletedEventArgs>? DownloadCompleted;

        public YtDlpService()
        {
            _ytDlpPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "yt-dlp.exe");
            _ffmpegPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ffmpeg.exe");
        }

        public async Task<bool> EnsureYtDlpAvailableAsync()
        {
            if (File.Exists(_ytDlpPath))
                return true;

            try
            {
                LogOutput?.Invoke(this, "Downloading yt-dlp...");
                await DownloadYtDlpAsync();
                return true;
            }
            catch (Exception ex)
            {
                LogOutput?.Invoke(this, $"Failed to download yt-dlp: {ex.Message}");
                return false;
            }
        }

        private async Task DownloadYtDlpAsync()
        {
            using var client = new HttpClient();
            var url = "https://github.com/yt-dlp/yt-dlp/releases/latest/download/yt-dlp.exe";
            var data = await client.GetByteArrayAsync(url);
            await File.WriteAllBytesAsync(_ytDlpPath, data);
        }

        public async Task<VideoInfo?> GetVideoInfoAsync(string url)
        {
            if (!await EnsureYtDlpAvailableAsync())
                return null;

            var arguments = $"--dump-json \"{url}\"";
            var result = await ExecuteYtDlpAsync(arguments);

            if (string.IsNullOrEmpty(result))
                return null;

            try
            {
                var videoInfo = JsonConvert.DeserializeObject<dynamic>(result);
                return new VideoInfo
                {
                    Title = videoInfo?.title ?? "",
                    Duration = TimeSpan.FromSeconds((double)(videoInfo?.duration ?? 0)).ToString(@"hh\:mm\:ss"),
                    TotalDuration = TimeSpan.FromSeconds((double)(videoInfo?.duration ?? 0))
                };
            }
            catch
            {
                return null;
            }
        }

        public async Task StartDownloadAsync(DownloadInfo downloadInfo)
        {
            if (!await EnsureYtDlpAvailableAsync())
                return;

            _cancellationTokenSource.CancelAfter(TimeSpan.FromHours(2)); // 2 hour timeout

            var tempDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "YtDlpWrapper", "temp_videos");
            var outputTemplate = Path.Combine(tempDir, "%(title)s.%(ext)s");

            var arguments = BuildDownloadArguments(downloadInfo, outputTemplate);
            
            try
            {
                await ExecuteYtDlpWithProgressAsync(arguments, downloadInfo);
            }
            catch (OperationCanceledException)
            {
                LogOutput?.Invoke(this, "Download cancelled by user.");
            }
            catch (Exception ex)
            {
                LogOutput?.Invoke(this, $"Download failed: {ex.Message}");
            }
        }

        private string BuildDownloadArguments(DownloadInfo downloadInfo, string outputTemplate)
        {
            var args = new List<string>
            {
                $"--output \"{outputTemplate}\"",
                "--merge-output-format mp4",
                // Force re-encoding the audio to AAC for maximum compatibility with WPF's MediaElement
                "--postprocessor-args \"-c:a aac\"",
                "--progress" // Use yt-dlp's built-in progress reporter
            };

            // Quality selection
            if (downloadInfo.Quality == "Best Quality")
            {
                // Download best video and best audio, merge with ffmpeg
                args.Add("-f bestvideo+bestaudio");
            }
            else if (downloadInfo.Quality == "audio-only")
            {
                args.Add("--extract-audio");
                args.Add("--audio-format aac");
            }
            else
            {
                args.Add($"--format \"best[height<={downloadInfo.Quality.Replace("p", "")}]\"");
            }

            // Format selection
            // Always merge to mp4, so skip custom format logic

            args.Add($"\"{downloadInfo.Url}\"");
            return string.Join(" ", args);
        }

        private async Task ExecuteYtDlpWithProgressAsync(string arguments, DownloadInfo downloadInfo)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = _ytDlpPath,
                Arguments = arguments,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            _currentProcess = new Process { StartInfo = startInfo };

            string? outputFilePath = null;
            string? tempDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "YtDlpWrapper", "temp_videos");

            _currentProcess.OutputDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    LogOutput?.Invoke(this, e.Data);
                    ParseProgress(e.Data, downloadInfo);

                    // More reliable way to get the final merged file path
                    if (e.Data.Contains("[Merger] Merging formats into"))
                    {
                        var match = Regex.Match(e.Data, "\"(.*?)\"");
                        if (match.Success)
                        {
                            outputFilePath = match.Groups[1].Value;
                            LogOutput?.Invoke(this, $"[YtDlpWrapper] Detected final merged file: {outputFilePath}");
                        }
                    }
                    // Fallback for non-merged files
                    else if (outputFilePath == null && e.Data.Contains("Destination:"))
                    {
                        var dest = e.Data.Split(new[] { "Destination:" }, StringSplitOptions.None).Last().Trim();
                        if (!string.IsNullOrEmpty(dest))
                        {
                            outputFilePath = dest;
                            LogOutput?.Invoke(this, $"[YtDlpWrapper] Detected destination file: {outputFilePath}");
                        }
                    }
                }
            };

            _currentProcess.ErrorDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    LogOutput?.Invoke(this, e.Data);
                }
            };

            _currentProcess.EnableRaisingEvents = true;
            _currentProcess.Exited += async (sender, e) =>
            {
                bool success = _currentProcess.ExitCode == 0;
                string? finalPath = null;
                if (success && !string.IsNullOrEmpty(outputFilePath))
                {
                    // Wait for the file to be available to prevent race condition
                    try
                    {
                        var timeout = TimeSpan.FromSeconds(10);
                        var sw = System.Diagnostics.Stopwatch.StartNew();
                        while (sw.Elapsed < timeout)
                        {
                            if (File.Exists(outputFilePath))
                            {
                                // Check if the file is not locked by another process.
                                using (FileStream stream = new FileInfo(outputFilePath).Open(FileMode.Open, FileAccess.Read, FileShare.None))
                                {
                                    stream.Close();
                                }
                                LogOutput?.Invoke(this, $"[YtDlpWrapper] File is ready: {outputFilePath}");
                                finalPath = outputFilePath;
                                break;
                            }
                            await Task.Delay(250);
                        }

                        if (finalPath == null)
                        {
                            LogOutput?.Invoke(this, $"[YtDlpWrapper] ERROR: File did not become available after 10s: {outputFilePath}");
                            success = false;
                        }
                    }
                    catch (IOException ex)
                    {
                        LogOutput?.Invoke(this, $"[YtDlpWrapper] ERROR: File is locked or unavailable: {ex.Message}");
                        success = false;
                    }
                }
                else if (success)
                {
                    LogOutput?.Invoke(this, "[YtDlpWrapper] ERROR: Process exited successfully, but no output file path was detected.");
                    success = false;
                }
                DownloadCompleted?.Invoke(this, new DownloadCompletedEventArgs { Success = success, FilePath = finalPath });
            };

            _currentProcess.Start();
            _currentProcess.BeginOutputReadLine();
            _currentProcess.BeginErrorReadLine();

            await _currentProcess.WaitForExitAsync(_cancellationTokenSource.Token);
        }

        private async Task<string> ExecuteYtDlpAsync(string arguments)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = _ytDlpPath,
                Arguments = arguments,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using var process = new Process { StartInfo = startInfo };
            process.Start();
            var output = await process.StandardOutput.ReadToEndAsync();
            await process.WaitForExitAsync();
            return output;
        }

        private void ParseProgress(string line, DownloadInfo downloadInfo)
        {
            // Try to parse the standard yt-dlp progress line
            // e.g., "[download]   1.1% of  127.24MiB at    2.44MiB/s ETA 00:51"
            var match = Regex.Match(line, @"\[download\]\s+(?<percent>[\d\.]+)%\s+of\s+~?(?<size>.*?)\s+at\s+(?<speed>.*?)\s+ETA\s+(?<eta>.*)");

            if (match.Success)
            {
                if (double.TryParse(match.Groups["percent"].Value, CultureInfo.InvariantCulture, out var progress))
                {
                    downloadInfo.Progress = progress;
                    downloadInfo.ProgressText = $"{progress:F1}%";
                }

                downloadInfo.Speed = match.Groups["speed"].Value.Trim();
                
                if (TimeSpan.TryParse(match.Groups["eta"].Value.Trim(), out var etaTime))
                {
                    downloadInfo.EstimatedTimeRemaining = etaTime;
                }

                ProgressChanged?.Invoke(this, new DownloadProgressEventArgs
                {
                    Progress = downloadInfo.Progress,
                    Speed = downloadInfo.Speed
                });
            }
        }

        public void CancelDownload()
        {
            _cancellationTokenSource.Cancel();
            _currentProcess?.Kill();
        }

        public void Dispose()
        {
            _currentProcess?.Dispose();
            _cancellationTokenSource.Dispose();
        }
    }

    public class DownloadProgressEventArgs : EventArgs
    {
        public double Progress { get; set; }
        public string Speed { get; set; } = string.Empty;
        public long DownloadedBytes { get; set; }
        public long TotalBytes { get; set; }
    }

    public class DownloadCompletedEventArgs : EventArgs
    {
        public bool Success { get; set; }
        public string? FilePath { get; set; }
    }
} 