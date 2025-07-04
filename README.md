# YT-DLP Wrapper

A modern, user-friendly Windows desktop application built with WPF that serves as a graphical interface for the powerful yt-dlp command-line video downloader. Download, preview, and crop videos from YouTube and other platforms with ease!

## ✨ Features

- **🎬 Full YouTube Player**: Embedded YouTube player with native controls and full functionality
- **🎥 Video Downloading**: Download videos from YouTube, Vimeo, and many other platforms
- **📺 Quality Selection**: Choose from 144p to 4K quality or "Best Available"
- **📱 Dual Player Mode**: Switch between YouTube player and downloaded video player
- **✂️ Video Cropping**: Extract specific segments with easy-to-use time controls
- **🔄 Session Management**: Clear video and reset application state for fresh starts
- **📊 Real-time Progress**: Live download progress and detailed logging
- **📁 Smart Folder Management**: Automatic folder opening to show downloaded and cropped videos
- **🌙 Modern Dark UI**: Sleek, dark-themed interface with rounded cards and modern design
- **🗂️ File Organization**: Automatic temp file cleanup and organized downloads with easy access
- **⚠️ Error Handling**: Comprehensive error logging and user-friendly error messages

## 🛠️ Prerequisites

Before running the application, you need to install these dependencies:

### Required Software

1. **yt-dlp**: Download from [https://github.com/yt-dlp/yt-dlp/releases](https://github.com/yt-dlp/yt-dlp/releases)
   - Download `yt-dlp.exe` and place it in the application directory
   
2. **FFmpeg**: Download from [https://ffmpeg.org/download.html](https://ffmpeg.org/download.html)
   - Download `ffmpeg.exe` and place it in the application directory
   
3. **.NET 6.0 Runtime**: Download from [https://dotnet.microsoft.com/download/dotnet/6.0](https://dotnet.microsoft.com/download/dotnet/6.0)
   - Install the Windows Desktop Runtime

4. **WebView2 Runtime**: Download from [https://developer.microsoft.com/microsoft-edge/webview2/](https://developer.microsoft.com/microsoft-edge/webview2/)
   - Required for YouTube player functionality (usually pre-installed on Windows 11)

### Directory Structure
```
YtDlpWrapper/
├── YtDlpWrapper.exe
├── yt-dlp.exe          # Download this
├── ffmpeg.exe          # Download this  
├── temp/               # Created automatically
├── downloads/          # Created automatically
└── error.log           # Created automatically
```

## 🚀 Installation

1. **Clone or download** this repository
2. **Install .NET 6.0** Desktop Runtime if not already installed
3. **Download yt-dlp.exe** and place it in the application folder
4. **Download ffmpeg.exe** and place it in the application folder
5. **Build the application**:
   ```bash
   dotnet build --configuration Release
   ```
6. **Run the application**:
   ```bash
   dotnet run
   ```

## 📖 Usage Guide

### Basic Workflow

1. **Enter Video URL**: Paste a video URL (YouTube, Vimeo, etc.) in the empty input field
2. **Load Video**: Click "Load Video" to preview the video (YouTube player loads instantly)
3. **Select Quality**: Choose your preferred download quality (144p to 4K or Best Available)
4. **Download**: Click "Download Now" to download the video for cropping
5. **Choose Player Mode**: 
   - **YouTube Player**: Watch with full YouTube controls (for preview)
   - **Downloaded Video**: Switch to local video for precise cropping (opens folder automatically)
6. **Set Crop Times**: Enter start and end times in HH:MM:SS format
7. **Crop Video**: Click "Crop Video" to extract the segment (opens downloads folder with cropped file)
8. **File Management**: Use "Open Downloads", "Clear Temp", or "Clear Video" buttons to manage your files
9. **Start Fresh**: Use "Clear Video" to reset everything and load a new video

### Player Mode Controls

- **🎬 Show YouTube Player**: Switch to embedded YouTube player with full controls (for preview)
- **📁 Show Downloaded + Open Folder**: Switch to local video player AND open the folder containing the downloaded video (for cropping)
- **🗑️ Clear Video**: Clear current video, reset all players, and start fresh

### Download Controls (Shown after loading a video)

- **📥 Download Quality Selector**: Choose from 144p to 4K or "Best Available"
- **📥 Download Now**: Download the video in selected quality for cropping purposes

### Quality Selection Options

- **🔥 Best Available**: Downloads the highest quality available (video + audio automatically merged)
- **📺 2160p (4K)**: Ultra High Definition (3840×2160) - *Separate video/audio download + merge*
- **🎬 1440p (2K)**: Quad HD (2560×1440) - *Separate video/audio download + merge*
- **📹 1080p (FHD)**: Full HD (1920×1080) - *Separate video/audio download + merge*
- **🎥 720p (HD)**: HD Ready (1280×720) - **Default** - *May use combined or separate streams*
- **📱 480p**: Standard Definition (854×480)
- **💾 360p**: Low Definition (640×360)
- **📞 240p**: Very Low (426×240)
- **⚡ 144p**: Minimal (256×144)

**Note**: For high quality downloads (1080p+), the app automatically downloads video and audio separately and merges them using ffmpeg. This ensures you get the actual quality selected, not a lower quality fallback.

### File Management Features

The application automatically opens relevant folders to help you find your files:

- **After loading**: Quality selection panel appears for you to choose download quality
- **After downloading**: "Show Downloaded + Open Folder" opens the temp folder and selects the downloaded video
- **After cropping**: Downloads folder opens automatically and selects your new cropped video
- **Manual access**: "Open Downloads" button gives you instant access to all saved videos
- **After clearing temp**: Temp folder opens to verify files were removed
- **Clear video**: "Clear Video" button resets all players, clears URL, hides quality panel, and restores default settings for a fresh start

### Local Video Player Controls (when viewing downloaded video)

- **▶️ Play**: Start video playback
- **⏸️ Pause**: Pause video playback  
- **⏹️ Stop**: Stop video playback

### Cropping & File Controls

- **Start Time**: Enter the beginning time of your desired segment (HH:MM:SS)
- **End Time**: Enter the ending time of your desired segment (HH:MM:SS)
- **🎬 Crop Video**: Extract the segment, save to downloads folder, and open folder to show the result
- **📁 Open Downloads**: Open the downloads folder to browse all saved videos
- **🗑️ Clear Temp**: Remove all temporary files and open temp folder for verification

### File Locations

- **Downloaded Videos**: Saved in the `downloads/` folder
- **Temporary Files**: Stored in the `temp/` folder (cleared automatically)
- **Error Logs**: Written to `error.log`

## 🔧 Configuration

### Video Quality
The application provides a quality selector dropdown with options from 144p to 4K. The default selection is **720p (HD)**. Simply choose your preferred quality from the dropdown before clicking "Load Video". The selected quality will be used for downloading and affects both file size and video clarity.

### Supported Formats
The application supports common video formats:
- MP4, MKV, WebM
- AVI, MOV, FLV

## 🐛 Troubleshooting

### Common Issues

**"yt-dlp.exe not found"**
- Ensure `yt-dlp.exe` is in the same folder as the application
- Download from the official releases page

**"ffmpeg.exe not found"**  
- Ensure `ffmpeg.exe` is in the same folder as the application
- Download from the official FFmpeg website

**"YouTube player failed to load"**
- Install WebView2 Runtime from Microsoft's official page
- WebView2 is usually pre-installed on Windows 11
- Restart the application after installing WebView2

**Video won't load**
- Check the log output for error messages
- Ensure the URL is valid and accessible
- Some videos may be geo-restricted or age-restricted

**Cropping fails**
- Verify time format is correct (HH:MM:SS)
- Ensure start time is before end time
- Check that the video file exists and isn't corrupted

### Error Logging
All errors are automatically logged to `error.log` in the application directory. Check this file for detailed error information.

## 🎯 System Requirements

- **OS**: Windows 10/11
- **Framework**: .NET 6.0 Desktop Runtime
- **Memory**: 4GB RAM minimum
- **Storage**: 1GB free space for temporary files

## 📝 License

This project is open source. The application uses:
- **yt-dlp**: [Unlicense](https://github.com/yt-dlp/yt-dlp/blob/master/LICENSE)
- **FFmpeg**: [LGPL/GPL](https://ffmpeg.org/legal.html)

## 🤝 Contributing

Contributions are welcome! Please feel free to submit issues and enhancement requests.

## 📞 Support

If you encounter any issues:
1. Check the `error.log` file for detailed error information
2. Ensure all dependencies are properly installed
3. Verify video URLs are accessible
4. Check that you have sufficient disk space

---

**Enjoy downloading and cropping your favorite videos! 🎬** 