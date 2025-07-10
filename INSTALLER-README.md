# 🚀 YT-DLP Wrapper Installer Setup Guide

This guide will help you create a professional Windows installer for the YT-DLP Wrapper application.

## 📋 Prerequisites

1. **Download & Install Inno Setup 6.4.3** (Free)
   - Visit: https://jrsoftware.org/isdl.php
   - Download: `innosetup-6.4.3.exe`
   - Install with default settings

2. **Ensure Application is Built**
   - Your app should be built in Release mode
   - Files should exist in: `bin\Release\net6.0-windows\win-x64\`

## 🎯 Quick Start (Automatic)

1. **Double-click** `build-installer.bat`
2. **Follow the prompts** - the script will:
   - ✅ Check if Inno Setup is installed
   - ✅ Build your application if needed
   - ✅ Check for dependencies (yt-dlp.exe, ffmpeg.exe)
   - ✅ Create the installer automatically
   - ✅ Offer to test the installer

3. **Done!** Your installer will be created in `installer-output\`

## 📁 Manual Setup (Advanced)

If you prefer manual control:

1. **Open Inno Setup**
2. **Open** `YT-DLP-Wrapper-Installer.iss`
3. **Modify settings** if needed:
   - Change version number (`#define MyAppVersion`)
   - Update publisher name (`#define MyAppPublisher`)
   - Modify descriptions
4. **Build** by pressing F9 or Build → Compile

## 🎨 Installer Features

Your installer includes:

### 📦 **Installation Features**
- ✅ Installs to `Program Files\YT-DLP Wrapper`
- ✅ Creates user data directories in `%AppData%`
- ✅ Includes all .NET dependencies
- ✅ Handles yt-dlp.exe and ffmpeg.exe intelligently

### 🔗 **Windows Integration**
- ✅ Desktop shortcut (optional)
- ✅ Start Menu entry
- ✅ Programs & Features uninstaller entry
- ✅ File associations for .ytdlp files
- ✅ Proper Windows UAC handling

### 🛡️ **Smart Dependency Handling**
- ✅ Checks for .NET 6.0 Runtime
- ✅ Offers to download .NET if missing
- ✅ Warns about missing dependencies
- ✅ Auto-downloads yt-dlp.exe on first run

### 💫 **Professional Experience**
- ✅ Modern wizard interface
- ✅ Custom icon and branding
- ✅ Progress indicators
- ✅ Compression for smaller file size
- ✅ Digital signature support (optional)

## 📊 File Output

After building, you'll get:

```
installer-output/
└── YT-DLP-Wrapper-Setup-v1.0.3.exe  (~15-25MB)
```

This single file contains everything users need!

## 📋 Distribution Checklist

Before distributing your installer:

- [ ] **Test the installer** on a clean Windows machine
- [ ] **Verify shortcuts** work correctly
- [ ] **Test uninstaller** removes everything
- [ ] **Check dependencies** download properly
- [ ] **Ensure icon** displays correctly
- [ ] **Validate admin permissions** work

## 🔧 Customization Options

### Change Version
```ini
#define MyAppVersion "1.0.4"  ; Update this line
```

### Change Publisher
```ini
#define MyAppPublisher "Your Company Name"
```

### Add License File
```ini
LicenseFile=LICENSE.txt  ; Uncomment and specify file
```

### Custom Branding
- Replace `icon.ico` with your custom icon
- Add wizard images for professional look

## 🎯 Advanced Features

### Silent Installation
Users can install silently:
```cmd
YT-DLP-Wrapper-Setup-v1.0.3.exe /SILENT
```

### Custom Install Location
```cmd
YT-DLP-Wrapper-Setup-v1.0.3.exe /DIR="C:\Custom\Path"
```

### No Desktop Icon
```cmd
YT-DLP-Wrapper-Setup-v1.0.3.exe /TASKS="!desktopicon"
```

## 🚨 Troubleshooting

### "Inno Setup not found"
- Ensure Inno Setup 6 is installed in default location
- Check both Program Files and Program Files (x86)

### "Application not built"
- Run `dotnet build --configuration Release`
- Ensure files exist in the Release directory

### Missing Dependencies
- yt-dlp.exe: Will auto-download on first run
- ffmpeg.exe: Download from https://ffmpeg.org/

## 📞 Support

If you encounter issues:
1. Check the Inno Setup documentation: https://jrsoftware.org/ishelp/
2. Verify all file paths in the .iss script
3. Test on a clean Windows VM

---

**🎉 Your professional installer is ready for distribution!** 