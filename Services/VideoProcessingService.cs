using System.Diagnostics;
using System.IO;
using YtDlpWrapper.Models;
using System.Globalization;

namespace YtDlpWrapper.Services
{
    public class VideoProcessingService
    {
        private readonly string _ffmpegPath;
        private Process? _currentProcess;

        public event EventHandler<string>? LogOutput;
        public event EventHandler<double>? ProgressChanged;
        public event EventHandler<bool>? ProcessingCompleted;

        public VideoProcessingService()
        {
            _ffmpegPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ffmpeg.exe");
        }

        public async Task<bool> EnsureFfmpegAvailableAsync()
        {
            if (File.Exists(_ffmpegPath))
                return true;

            try
            {
                LogOutput?.Invoke(this, "FFmpeg not found. Please download FFmpeg and place ffmpeg.exe in the application directory.");
                return false;
            }
            catch (Exception ex)
            {
                LogOutput?.Invoke(this, $"FFmpeg error: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> CropVideoAsync(string inputPath, string outputPath, CropRange cropRange)
        {
            if (!await EnsureFfmpegAvailableAsync())
                return false;

            if (!cropRange.IsValid)
            {
                LogOutput?.Invoke(this, "Invalid crop range. Start time must be less than end time.");
                return false;
            }

            var duration = cropRange.Duration.TotalSeconds;
            var startTime = cropRange.StartTime.TotalSeconds;

            var arguments = $"-i \"{inputPath}\" -ss {startTime.ToString(CultureInfo.InvariantCulture)} -t {duration.ToString(CultureInfo.InvariantCulture)} -c copy \"{outputPath}\"";

            try
            {
                await ExecuteFfmpegAsync(arguments);
                ProcessingCompleted?.Invoke(this, true);
                return true;
            }
            catch (Exception ex)
            {
                LogOutput?.Invoke(this, $"Video cropping failed: {ex.Message}");
                ProcessingCompleted?.Invoke(this, false);
                return false;
            }
        }

        public async Task<bool> ConvertVideoAsync(string inputPath, string outputPath, string format = "mp4")
        {
            if (!await EnsureFfmpegAvailableAsync())
                return false;

            var arguments = $"-i \"{inputPath}\" -c:v libx264 -c:a aac \"{outputPath}\"";

            try
            {
                await ExecuteFfmpegAsync(arguments);
                ProcessingCompleted?.Invoke(this, true);
                return true;
            }
            catch (Exception ex)
            {
                LogOutput?.Invoke(this, $"Video conversion failed: {ex.Message}");
                ProcessingCompleted?.Invoke(this, false);
                return false;
            }
        }

        public async Task<TimeSpan?> GetVideoDurationAsync(string filePath)
        {
            if (!await EnsureFfmpegAvailableAsync())
                return null;

            var arguments = $"-i \"{filePath}\" -show_entries format=duration -v quiet -of csv=\"p=0\"";

            try
            {
                var result = await ExecuteFfmpegAsync(arguments);
                if (double.TryParse(result.Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out var duration))
                {
                    return TimeSpan.FromSeconds(duration);
                }
            }
            catch (Exception ex)
            {
                LogOutput?.Invoke(this, $"Failed to get video duration: {ex.Message}");
            }

            return null;
        }

        private async Task<string> ExecuteFfmpegAsync(string arguments)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = _ffmpegPath,
                Arguments = arguments,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            _currentProcess = new Process { StartInfo = startInfo };

            var output = new System.Text.StringBuilder();
            var error = new System.Text.StringBuilder();

            _currentProcess.OutputDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    output.AppendLine(e.Data);
                    LogOutput?.Invoke(this, e.Data);
                }
            };

            _currentProcess.ErrorDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    error.AppendLine(e.Data);
                    LogOutput?.Invoke(this, e.Data);
                    ParseFfmpegProgress(e.Data);
                }
            };

            _currentProcess.Start();
            _currentProcess.BeginOutputReadLine();
            _currentProcess.BeginErrorReadLine();

            await _currentProcess.WaitForExitAsync();

            if (_currentProcess.ExitCode != 0)
            {
                throw new Exception($"FFmpeg failed with exit code {_currentProcess.ExitCode}: {error}");
            }

            return output.ToString();
        }

        private void ParseFfmpegProgress(string line)
        {
            // Parse FFmpeg progress output (time=00:01:23.45)
            var match = System.Text.RegularExpressions.Regex.Match(line, @"time=(\d{2}):(\d{2}):(\d{2})\.(\d{2})");
            if (match.Success)
            {
                var hours = int.Parse(match.Groups[1].Value);
                var minutes = int.Parse(match.Groups[2].Value);
                var seconds = int.Parse(match.Groups[3].Value);
                var centiseconds = int.Parse(match.Groups[4].Value);

                var currentTime = new TimeSpan(0, hours, minutes, seconds, centiseconds * 10);
                // Note: We don't have total duration in FFmpeg output, so we can't calculate percentage
                // ProgressChanged?.Invoke(this, progress);
            }
        }

        public void CancelProcessing()
        {
            _currentProcess?.Kill();
        }

        public void Dispose()
        {
            _currentProcess?.Dispose();
        }
    }
} 