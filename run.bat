@echo off
echo Starting YT-DLP Wrapper...
echo.

:: Build the application first
echo Building application...
dotnet build --configuration Debug
if %errorlevel% neq 0 (
    echo Build failed!
    pause
    exit /b 1
)

:: Check if dependencies exist in build directory
if not exist "bin\Debug\net6.0-windows\win-x64\yt-dlp.exe" (
    echo ERROR: yt-dlp.exe not found in build directory!
    echo Please run setup.bat first to install dependencies.
    pause
    exit /b 1
)

if not exist "bin\Debug\net6.0-windows\win-x64\ffmpeg.exe" (
    echo ERROR: ffmpeg.exe not found in build directory!
    echo Please run setup.bat first to install dependencies.
    pause
    exit /b 1
)

:: Run the application from the correct directory
echo Starting application from build directory...
cd "bin\Debug\net6.0-windows\win-x64"
start "" "YtDlpWrapper.exe"
cd ..\..\..\..\

echo.
echo Application started.
pause 