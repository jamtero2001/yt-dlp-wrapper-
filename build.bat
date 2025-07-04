@echo off
title YT-DLP Wrapper - Build Script
color 0B

echo.
echo ================================
echo  YT-DLP Wrapper Build Script
echo ================================
echo.

echo [1/3] Creating application icon...
powershell -ExecutionPolicy Bypass -File "create-icon.ps1"

echo.
echo [2/3] Building distributable package...
powershell -ExecutionPolicy Bypass -File "build-release.ps1" -Version "1.0.0"

echo.
echo [3/3] Build complete!
echo.
echo Your distributable files are in the 'dist' folder:
echo - YtDlpWrapper-v1.0.0-Portable.zip (ready to share!)
echo.

if exist "dist\YtDlpWrapper-v1.0.0-Portable.zip" (
    echo ✅ SUCCESS: Your app is ready for distribution!
    echo.
    echo Next steps:
    echo 1. Download yt-dlp.exe and ffmpeg.exe to this folder
    echo 2. Run this script again to include them
    echo 3. Share the ZIP file with users
) else (
    echo ❌ Build may have failed. Check the output above.
)

echo.
echo Press any key to open the dist folder...
pause >nul
start "" "dist" 