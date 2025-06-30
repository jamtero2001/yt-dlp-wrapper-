# yt-dlp Wrapper - C# WPF Application

A modern Windows WPF application wrapper for yt-dlp with Material Design UI, featuring video downloading, playback, and cropping capabilities.

## Features

### Download Management
- **URL Input**: Paste/clear buttons for easy URL management
- **Quality Selection**: 1080p, 720p, 480p, audio-only options
- **Format Selection**: mp4, webm, mkv formats
- **Download Directory**: Browse button for custom download locations
- **Progress Tracking**: Real-time progress bar with percentage and speed
- **Download Control**: Cancel functionality and clear log options

### Video Player & Cropping
- **MediaElement**: Full video playback capabilities
- **Custom Time Bar**: YouTube-style interface with:
  - Play/pause controls
  - Current/total time display
  - Draggable start/end thumbs for crop range selection
  - Moving playhead during playback
- **Crop & Export**: Time range validation and export functionality

## Technical Architecture

### Core Technologies
- **.NET 8**: Latest framework for optimal performance
- **WPF**: Rich desktop application framework
- **MVVM Pattern**: Using CommunityToolkit.Mvvm for clean architecture
- **Material Design**: Modern UI with MaterialDesignThemes.Wpf

### Key Dependencies
- `CommunityToolkit.Mvvm` - MVVM implementation
- `MaterialDesignThemes.Wpf` - Material Design UI components
- `Newtonsoft.Json` - JSON handling
- `Microsoft.Xaml.Behaviors` - XAML behaviors

### File Structure
```
yt-dlp-wrapper/
├── Models/
│   ├── DownloadInfo.cs
│   ├── VideoInfo.cs
│   └── CropRange.cs
├── ViewModels/
│   ├── MainViewModel.cs
│   ├── DownloadViewModel.cs
│   └── PlayerViewModel.cs
├── Views/
│   ├── MainWindow.xaml
│   ├── DownloadPanel.xaml
│   └── VideoPlayer.xaml
├── Services/
│   ├── YtDlpService.cs
│   ├── DownloadService.cs
│   └── VideoProcessingService.cs
├── Controls/
│   ├── CustomTimeBar.xaml
│   └── ProgressIndicator.xaml
└── App.xaml
```

## Implementation Notes

### Critical Technical Considerations
1. **Video/Audio Merging**: yt-dlp often downloads video and audio separately for high quality. The app uses `--merge-output-format mp4` flag for proper merging.
2. **Async/Await Patterns**: Proper implementation for UI responsiveness during downloads.
3. **Process Management**: Correct handling of yt-dlp process cancellation.
4. **Progress Parsing**: Regex-based parsing of yt-dlp output for real-time progress updates.
5. **File Management**: Downloads to `temp_videos` folder first, then moves to selected directory.
6. **Error Handling**: Comprehensive error handling with user feedback.

### UI/UX Requirements
- **Modern Material Design**: Professional aesthetic with consistent theming
- **Responsive Layout**: Adapts to different screen sizes
- **Visual Feedback**: Clear indicators for all operations
- **Intuitive Controls**: User-friendly video player interface
- **Smooth Interactions**: Professional time bar with fluid interactions

### Platform Support
- **Windows 10/11**: Primary target platform
- **Video Platforms**: YouTube and other yt-dlp supported platforms
- **File Formats**: mp4, webm, mkv with automatic format detection

## Getting Started

1. **Prerequisites**: .NET 8 SDK installed
2. **Build**: `dotnet build`
3. **Run**: `dotnet run`

## Usage

1. **Download Videos**:
   - Paste URL in the input field
   - Select quality and format preferences
   - Choose download directory
   - Click download and monitor progress

2. **Video Playback**:
   - Load downloaded videos in the player
   - Use standard media controls
   - Navigate through video timeline

3. **Video Cropping**:
   - Set start and end points using draggable thumbs
   - Validate time range (start < end)
   - Export cropped video segment

## Development

### Architecture Patterns
- **MVVM**: Clean separation of concerns
- **Dependency Injection**: Service-based architecture
- **Command Pattern**: UI interaction handling
- **Observer Pattern**: Progress and status updates

### Best Practices
- Proper async/await usage for non-blocking operations
- Comprehensive error handling and user feedback
- Memory management for large video files
- Cross-platform file path handling
- Regular expression for log parsing 