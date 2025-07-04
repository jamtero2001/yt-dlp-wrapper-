@echo off
echo Starting YT-DLP Wrapper...
echo.

:: Check if dependencies exist
if not exist "yt-dlp.exe" (
    echo ERROR: yt-dlp.exe not found!
    echo Please run setup.bat first to install dependencies.
    pause
    exit /b 1
)

if not exist "ffmpeg.exe" (
    echo ERROR: ffmpeg.exe not found!
    echo Please run setup.bat first to install dependencies.
    pause
    exit /b 1
)

:: Run the application
dotnet run

echo.
echo Application closed.
pause 