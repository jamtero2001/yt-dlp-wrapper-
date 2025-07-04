# YT-DLP Wrapper - Professional Build Script
# This script creates a complete, distributable package

param(
    [string]$Version = "1.0.0",
    [switch]$CreateInstaller = $false
)

Write-Host "🚀 Building YT-DLP Wrapper v$Version..." -ForegroundColor Cyan
Write-Host "=================================================" -ForegroundColor Cyan

# Configuration
$AppName = "YtDlpWrapper"
$OutputDir = "dist"
$TempDir = "build-temp"
$PublishDir = "$TempDir\publish"

# Clean previous builds
Write-Host "🧹 Cleaning previous builds..." -ForegroundColor Yellow
if (Test-Path $OutputDir) { Remove-Item $OutputDir -Recurse -Force }
if (Test-Path $TempDir) { Remove-Item $TempDir -Recurse -Force }
New-Item -ItemType Directory -Path $OutputDir -Force | Out-Null
New-Item -ItemType Directory -Path $TempDir -Force | Out-Null

# Step 1: Create Icon
Write-Host "🎨 Creating application icon..." -ForegroundColor Yellow
try {
    if (-not (Test-Path "icon.ico")) {
        & .\create-icon.ps1
    } else {
        Write-Host "✅ Icon already exists" -ForegroundColor Green
    }
} catch {
    Write-Host "⚠️ Icon creation failed, continuing without custom icon" -ForegroundColor Yellow
}

# Step 2: Download Dependencies
Write-Host "📦 Downloading dependencies..." -ForegroundColor Yellow

# Download yt-dlp if not present
if (-not (Test-Path "yt-dlp.exe")) {
    Write-Host "⬇️ Downloading yt-dlp..." -ForegroundColor Cyan
    try {
        $ytdlpUrl = "https://github.com/yt-dlp/yt-dlp/releases/latest/download/yt-dlp.exe"
        Invoke-WebRequest -Uri $ytdlpUrl -OutFile "yt-dlp.exe" -UseBasicParsing
        Write-Host "✅ yt-dlp.exe downloaded" -ForegroundColor Green
    } catch {
        Write-Host "❌ Failed to download yt-dlp.exe: $($_.Exception.Message)" -ForegroundColor Red
        Write-Host "Please manually download from: https://github.com/yt-dlp/yt-dlp/releases" -ForegroundColor Yellow
    }
} else {
    Write-Host "✅ yt-dlp.exe already exists" -ForegroundColor Green
}

# Download FFmpeg if not present
if (-not (Test-Path "ffmpeg.exe")) {
    Write-Host "⬇️ Downloading FFmpeg..." -ForegroundColor Cyan
    try {
        # Note: This is a simplified download. For production, you might want to use official builds
        Write-Host "⚠️ Please manually download FFmpeg from https://ffmpeg.org/download.html" -ForegroundColor Yellow
        Write-Host "   Extract ffmpeg.exe to this folder" -ForegroundColor Yellow
    } catch {
        Write-Host "❌ FFmpeg download helper failed" -ForegroundColor Red
    }
} else {
    Write-Host "✅ ffmpeg.exe already exists" -ForegroundColor Green
}

# Step 3: Build Self-Contained Application
Write-Host "🔨 Building self-contained application..." -ForegroundColor Yellow
try {
    $publishArgs = @(
        "publish"
        "--configuration", "Release"
        "--runtime", "win-x64"
        "--self-contained", "true"
        "--output", $PublishDir
        "/p:PublishSingleFile=true"
        "/p:IncludeNativeLibrariesForSelfExtract=true"
        "/p:PublishReadyToRun=true"
        "/p:AssemblyVersion=$Version"
        "/p:FileVersion=$Version"
    )
    
    & dotnet @publishArgs
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "✅ Build successful!" -ForegroundColor Green
    } else {
        throw "Build failed with exit code $LASTEXITCODE"
    }
} catch {
    Write-Host "❌ Build failed: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

# Step 4: Create Distribution Package
Write-Host "📦 Creating distribution package..." -ForegroundColor Yellow

$PackageDir = "$OutputDir\YtDlpWrapper-v$Version"
New-Item -ItemType Directory -Path $PackageDir -Force | Out-Null

# Copy main executable
Copy-Item "$PublishDir\YtDlpWrapper.exe" $PackageDir -Force

# Copy dependencies (if they exist)
$dependencies = @("yt-dlp.exe", "ffmpeg.exe")
foreach ($dep in $dependencies) {
    if (Test-Path $dep) {
        Copy-Item $dep $PackageDir -Force
        Write-Host "✅ Included $dep" -ForegroundColor Green
    } else {
        Write-Host "⚠️ Missing $dep - users will need to download separately" -ForegroundColor Yellow
    }
}

# Copy documentation
if (Test-Path "README.md") {
    Copy-Item "README.md" $PackageDir -Force
}

# Create user-friendly launcher script
$LauncherContent = @"
@echo off
title YT-DLP Wrapper v$Version
echo Starting YT-DLP Wrapper...
echo.

REM Check for dependencies
if not exist "yt-dlp.exe" (
    echo ERROR: yt-dlp.exe not found!
    echo Please download from: https://github.com/yt-dlp/yt-dlp/releases
    echo.
    pause
    exit /b 1
)

if not exist "ffmpeg.exe" (
    echo WARNING: ffmpeg.exe not found!
    echo Some features may not work. Download from: https://ffmpeg.org/download.html
    echo.
)

REM Start the application
YtDlpWrapper.exe
"@

$LauncherContent | Out-File -FilePath "$PackageDir\Start-YtDlpWrapper.bat" -Encoding ASCII

# Create installation instructions
$InstructionsContent = @"
# YT-DLP Wrapper v$Version Installation Instructions

## Quick Start
1. Double-click 'Start-YtDlpWrapper.bat' to run the application
2. Or run 'YtDlpWrapper.exe' directly

## First Time Setup
If you encounter missing dependency errors:

### Required Dependencies:
1. **yt-dlp.exe** - Download from: https://github.com/yt-dlp/yt-dlp/releases
2. **ffmpeg.exe** - Download from: https://ffmpeg.org/download.html

### WebView2 Runtime (if needed):
- Download from: https://developer.microsoft.com/microsoft-edge/webview2/

## Features
- Modern SwarmAI-inspired dark theme
- YouTube video preview
- Multiple quality options (144p to 4K)
- Video downloading with audio merging
- Simple, intuitive interface

## System Requirements
- Windows 10/11 (64-bit)
- .NET 6.0 Runtime (included)
- WebView2 Runtime
- Internet connection

## Troubleshooting
- Run as Administrator if you encounter permission issues
- Ensure antivirus isn't blocking the executable
- Check Windows Defender SmartScreen settings

Enjoy your new video downloader! 🎉
"@

$InstructionsContent | Out-File -FilePath "$PackageDir\INSTALLATION.md" -Encoding UTF8

# Create ZIP package
Write-Host "🗜️ Creating ZIP archive..." -ForegroundColor Yellow
$ZipPath = "$OutputDir\YtDlpWrapper-v$Version-Portable.zip"
try {
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    [System.IO.Compression.ZipFile]::CreateFromDirectory($PackageDir, $ZipPath)
    Write-Host "✅ Created $ZipPath" -ForegroundColor Green
} catch {
    Write-Host "❌ Failed to create ZIP: $($_.Exception.Message)" -ForegroundColor Red
}

# Step 5: Create Installer (Optional)
if ($CreateInstaller) {
    Write-Host "🔧 Creating installer..." -ForegroundColor Yellow
    
    # Check for NSIS
    $nsisPath = "${env:ProgramFiles(x86)}\NSIS\makensis.exe"
    if (-not (Test-Path $nsisPath)) {
        $nsisPath = "${env:ProgramFiles}\NSIS\makensis.exe"
    }
    
    if (Test-Path $nsisPath) {
        # Create NSIS script
        $nsisScript = @"
!define APP_NAME "YT-DLP Wrapper"
!define APP_VERSION "$Version"
!define APP_PUBLISHER "Your Company"
!define APP_EXECUTABLE "YtDlpWrapper.exe"

Name "`${APP_NAME} v`${APP_VERSION}"
OutFile "$OutputDir\YtDlpWrapper-v$Version-Setup.exe"
InstallDir "`$PROGRAMFILES64\YtDlpWrapper"
RequestExecutionLevel admin

Section "Main Application"
    SetOutPath "`$INSTDIR"
    File "$PackageDir\*.*"
    
    CreateDirectory "`$SMPROGRAMS\YT-DLP Wrapper"
    CreateShortCut "`$SMPROGRAMS\YT-DLP Wrapper\YT-DLP Wrapper.lnk" "`$INSTDIR\`${APP_EXECUTABLE}"
    CreateShortCut "`$DESKTOP\YT-DLP Wrapper.lnk" "`$INSTDIR\`${APP_EXECUTABLE}"
    
    WriteUninstaller "`$INSTDIR\Uninstall.exe"
SectionEnd

Section "Uninstall"
    Delete "`$INSTDIR\*.*"
    RMDir "`$INSTDIR"
    Delete "`$SMPROGRAMS\YT-DLP Wrapper\*.*"
    RMDir "`$SMPROGRAMS\YT-DLP Wrapper"
    Delete "`$DESKTOP\YT-DLP Wrapper.lnk"
SectionEnd
"@
        
        $nsisScript | Out-File -FilePath "$TempDir\installer.nsi" -Encoding UTF8
        
        try {
            & $nsisPath "$TempDir\installer.nsi"
            Write-Host "✅ Installer created!" -ForegroundColor Green
        } catch {
            Write-Host "❌ Installer creation failed: $($_.Exception.Message)" -ForegroundColor Red
        }
    } else {
        Write-Host "⚠️ NSIS not found. Download from: https://nsis.sourceforge.io/" -ForegroundColor Yellow
    }
}

# Cleanup
Remove-Item $TempDir -Recurse -Force

# Summary
Write-Host "`n🎉 Build Complete!" -ForegroundColor Green
Write-Host "=================================================" -ForegroundColor Green
Write-Host "📁 Output directory: $OutputDir" -ForegroundColor Cyan
Write-Host "📦 Portable package: YtDlpWrapper-v$Version-Portable.zip" -ForegroundColor Cyan

if ($CreateInstaller -and (Test-Path "$OutputDir\YtDlpWrapper-v$Version-Setup.exe")) {
    Write-Host "🔧 Installer: YtDlpWrapper-v$Version-Setup.exe" -ForegroundColor Cyan
}

Write-Host "`n📋 Distribution Checklist:" -ForegroundColor Yellow
Write-Host "✅ Application executable" -ForegroundColor Green
Write-Host "✅ Installation instructions" -ForegroundColor Green
Write-Host "✅ Launcher script" -ForegroundColor Green

if (Test-Path "$PackageDir\yt-dlp.exe") {
    Write-Host "✅ yt-dlp.exe included" -ForegroundColor Green
} else {
    Write-Host "⚠️ yt-dlp.exe missing - users need to download" -ForegroundColor Yellow
}

if (Test-Path "$PackageDir\ffmpeg.exe") {
    Write-Host "✅ ffmpeg.exe included" -ForegroundColor Green
} else {
    Write-Host "⚠️ ffmpeg.exe missing - users need to download" -ForegroundColor Yellow
}

Write-Host "`nReady for distribution! 🚀" -ForegroundColor Green 