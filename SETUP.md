# yt-dlp Wrapper Setup Guide

## Prerequisites

1. **.NET 8 SDK** - Download and install from [Microsoft's official site](https://dotnet.microsoft.com/download/dotnet/8.0)
2. **FFmpeg** - Required for video processing (optional but recommended)

## Installation

### Option 1: Build from Source

1. **Clone or download** this repository
2. **Open a terminal/command prompt** in the project directory
3. **Restore packages**:
   ```bash
   dotnet restore
   ```
4. **Build the application**:
   ```bash
   dotnet build --configuration Release
   ```
5. **Run the application**:
   ```bash
   dotnet run --configuration Release
   ```

### Option 2: Using Build Scripts

#### Windows (Command Prompt):
```cmd
build.bat
```

#### Windows (PowerShell):
```powershell
.\build.ps1
```

## FFmpeg Setup (Optional but Recommended)

For video cropping functionality, you need FFmpeg:

1. **Download FFmpeg** from [ffmpeg.org](https://ffmpeg.org/download.html)
2. **Extract the archive** and find `ffmpeg.exe`
3. **Place `ffmpeg.exe`** in the same directory as the application executable
4. **Alternative**: Add FFmpeg to your system PATH

## First Run

1. **Launch the application**
2. **yt-dlp will be automatically downloaded** on first use
3. **The application creates a temp directory** at:
   ```
   %LOCALAPPDATA%\YtDlpWrapper\temp_videos\
   ```

## Usage

### Downloading Videos

1. **Paste a video URL** (YouTube, Vimeo, etc.)
2. **Select quality** (1080p, 720p, 480p, audio-only)
3. **Choose format** (mp4, webm, mkv)
4. **Set download directory** (optional)
5. **Click Download** and monitor progress

### Video Playback & Cropping

1. **Load a video** using the "Load Video" button
2. **Use the custom time bar** to:
   - Play/pause video
   - Navigate through timeline
   - Set crop start/end points
3. **Drag the thumbs** to adjust crop range
4. **Click "Crop & Export"** to save the cropped segment

## Features

### Download Management
- ✅ URL input with paste/clear buttons
- ✅ Quality selection (1080p, 720p, 480p, audio-only)
- ✅ Format selection (mp4, webm, mkv)
- ✅ Download directory selection
- ✅ Real-time progress tracking
- ✅ Download cancellation
- ✅ Log output with timestamps

### Video Player & Cropping
- ✅ MediaElement for video playback
- ✅ Custom time bar with draggable controls
- ✅ Play/pause functionality
- ✅ Time range selection for cropping
- ✅ Crop and export functionality
- ✅ Time range validation

### Technical Features
- ✅ Automatic yt-dlp download/management
- ✅ Video/audio merging with FFmpeg
- ✅ Temp file management
- ✅ Error handling and user feedback
- ✅ Progress parsing with regex
- ✅ Modern Material Design UI

## Troubleshooting

### Common Issues

1. **"yt-dlp not found"**
   - The application will automatically download yt-dlp on first use
   - Check your internet connection

2. **"FFmpeg not found"**
   - Download FFmpeg and place `ffmpeg.exe` in the app directory
   - Or add FFmpeg to your system PATH

3. **Download fails**
   - Check the URL is valid
   - Verify internet connection
   - Check the log output for specific errors

4. **Video won't play**
   - Ensure the video file is not corrupted
   - Try a different video format
   - Check if the file path contains special characters

### Log Files

The application logs to the console output. For debugging:
1. **Run from command line** to see detailed logs
2. **Check the log panel** in the application
3. **Look for error messages** in the status bar

## Development

### Project Structure
```
YtDlpWrapper/
├── Models/           # Data models
├── ViewModels/       # MVVM view models
├── Views/           # WPF views
├── Services/        # Business logic services
├── Controls/        # Custom controls
├── Converters/      # Value converters
└── Styles/          # XAML styles
```

### Key Dependencies
- `CommunityToolkit.Mvvm` - MVVM implementation
- `MaterialDesignThemes.Wpf` - Material Design UI
- `Newtonsoft.Json` - JSON handling
- `Microsoft.Xaml.Behaviors` - XAML behaviors

### Building for Distribution

1. **Publish the application**:
   ```bash
   dotnet publish --configuration Release --self-contained --runtime win-x64
   ```

2. **Include required files**:
   - `yt-dlp.exe` (auto-downloaded)
   - `ffmpeg.exe` (user-provided)

## License

This project is open source. See the LICENSE file for details.

## Support

For issues and questions:
1. Check the troubleshooting section
2. Review the log output
3. Create an issue on the project repository 