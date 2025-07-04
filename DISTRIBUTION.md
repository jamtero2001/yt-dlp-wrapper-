# 🚀 YT-DLP Wrapper - Distribution Guide

This guide shows you how to create a professional, distributable version of your YT-DLP Wrapper that users can download and run on their computers.

## 🎯 Quick Start (3 Steps)

### 1. Create App Icon & Build
```powershell
# Create the app icon
.\create-icon.ps1

# Build distributable package
.\build-release.ps1
```

### 2. Download Dependencies (Manual)
Download these files and place them in your project folder:
- **yt-dlp.exe**: https://github.com/yt-dlp/yt-dlp/releases/latest
- **ffmpeg.exe**: https://ffmpeg.org/download.html (extract from zip)

### 3. Create Final Package
```powershell
# Build final package with all dependencies
.\build-release.ps1 -Version "1.0.0"

# Optional: Create installer too
.\build-release.ps1 -Version "1.0.0" -CreateInstaller
```

## 📦 What Gets Created

After running the build script, you'll have:

```
dist/
├── YtDlpWrapper-v1.0.0/           # Portable folder
│   ├── YtDlpWrapper.exe           # Main application (self-contained)
│   ├── yt-dlp.exe                 # Video downloader
│   ├── ffmpeg.exe                 # Video processing
│   ├── Start-YtDlpWrapper.bat     # User-friendly launcher
│   ├── INSTALLATION.md            # User instructions
│   └── README.md                  # Documentation
├── YtDlpWrapper-v1.0.0-Portable.zip  # Ready to distribute!
└── YtDlpWrapper-v1.0.0-Setup.exe     # Installer (if created)
```

## 🎨 Custom Logo/Icon

### Option 1: Auto-Generated Icon
The `create-icon.ps1` script creates a SwarmAI-style icon automatically:
- Dark gradient background
- Orange circle
- "YT" text in white

### Option 2: Custom Icon
1. Create your own icon (256x256 pixels recommended)
2. Save as `icon.ico` in the project folder
3. The build script will automatically use it

### Option 3: Online Icon Makers
- **Favicon.io**: Free icon generator
- **Canva**: Professional design tools
- **IconArchive**: Free icon downloads

**Recommended colors**: Use orange (#FF6B35) and dark theme colors to match your app's design.

## 🔧 Distribution Options

### Option A: Portable ZIP (Recommended)
**Best for**: Simple distribution, no installation required
```powershell
.\build-release.ps1 -Version "1.0.0"
```
- Creates `YtDlpWrapper-v1.0.0-Portable.zip`
- Users extract and run
- No admin rights needed
- Easy to share

### Option B: Windows Installer
**Best for**: Professional distribution, auto-updates
```powershell
# Install NSIS first: https://nsis.sourceforge.io/
.\build-release.ps1 -Version "1.0.0" -CreateInstaller
```
- Creates `YtDlpWrapper-v1.0.0-Setup.exe`
- Professional installation experience
- Start menu shortcuts
- Uninstaller included

## 📤 Sharing Your App

### GitHub Releases (Recommended)
1. Create a GitHub repository for your project
2. Go to "Releases" → "Create a new release"
3. Upload your ZIP/installer files
4. Add release notes describing features

### Other Distribution Methods
- **Google Drive/OneDrive**: Share the ZIP file
- **Website**: Host the download on your website
- **Windows Package Manager**: Submit to winget (advanced)
- **Microsoft Store**: Package as MSIX (advanced)

## 👥 User Instructions Template

Share this with your users:

```markdown
# YT-DLP Wrapper - Installation

## Download
Download the latest version: [YtDlpWrapper-v1.0.0-Portable.zip]

## Installation
1. Extract the ZIP file to any folder
2. Double-click "Start-YtDlpWrapper.bat"
3. Enjoy your new video downloader!

## Requirements
- Windows 10/11 (64-bit)
- Internet connection
- WebView2 Runtime (usually pre-installed)

## Features
✅ SwarmAI-inspired modern design
✅ YouTube video preview
✅ Quality selection (144p to 4K)
✅ Fast downloads with audio merging
✅ Simple, intuitive interface
```

## 🛠️ Advanced Customization

### Version Management
```powershell
# Update version in multiple places
.\build-release.ps1 -Version "1.2.3"
```

### Branding
1. Update `AssemblyCompany` in `YtDlpWrapper.csproj`
2. Replace "Your Company" with your name/organization
3. Update copyright information

### Code Signing (Professional)
For trusted distribution:
1. Get a code signing certificate
2. Sign your executable with `signtool.exe`
3. Users won't see "Unknown Publisher" warnings

### Auto-Updates
Consider adding update checking to your app:
- Check GitHub releases API
- Download and replace executable
- Notify users of new versions

## 🚨 Important Notes

### Antivirus Software
- Some antivirus programs may flag self-contained executables
- Consider code signing to reduce false positives
- Test on multiple antivirus solutions

### Dependencies
- **yt-dlp.exe**: Essential for downloading videos
- **ffmpeg.exe**: Required for video processing/merging
- **WebView2**: Usually pre-installed on Windows 10/11

### File Size
- Self-contained .NET apps are ~70-100MB
- This includes the entire .NET runtime
- Trade-off for "no installation required"

## 📊 Distribution Checklist

Before releasing:
- [ ] Test on clean Windows machine
- [ ] Verify all dependencies are included
- [ ] Test with different video URLs
- [ ] Check antivirus detection
- [ ] Create clear user instructions
- [ ] Test installer (if created)
- [ ] Prepare release notes

## 🆘 Troubleshooting

### Build Issues
```powershell
# Clear and rebuild
dotnet clean
dotnet restore
.\build-release.ps1
```

### Missing Dependencies
- Download manually from official sources
- Place in project root folder
- Re-run build script

### Icon Issues
- Try running PowerShell as Administrator
- Use online ICO converter as backup
- Verify icon.ico is in project root

## 🎉 Success!

Once you've followed this guide, you'll have a professional, distributable application that users can easily download and run. Your YT-DLP Wrapper will look as polished as commercial software!

**Ready to share your creation with the world!** 🌟 