@echo off
echo ====================================
echo     YT-DLP Wrapper Setup Script
echo ====================================
echo.

:: Create directories
echo Creating directories...
if not exist "temp" mkdir temp
if not exist "downloads" mkdir downloads
echo Directories created successfully.
echo.

:: Check if yt-dlp.exe exists
if exist "yt-dlp.exe" (
    echo yt-dlp.exe already exists.
) else (
    echo Downloading yt-dlp.exe...
    echo Please download yt-dlp.exe manually from:
    echo https://github.com/yt-dlp/yt-dlp/releases
    echo.
    echo Download the Windows executable and place it in this folder.
    echo Press any key when you've downloaded yt-dlp.exe...
    pause >nul
)

:: Check if ffmpeg.exe exists
if exist "ffmpeg.exe" (
    echo ffmpeg.exe already exists.
) else (
    echo ffmpeg.exe not found.
    echo Please download ffmpeg.exe from:
    echo https://ffmpeg.org/download.html
    echo.
    echo Download the Windows executable and place it in this folder.
    echo Press any key when you've downloaded ffmpeg.exe...
    pause >nul
)

:: Check for .NET 6.0
echo.
echo Checking for .NET 6.0 Runtime...
dotnet --version >nul 2>&1
if %errorlevel% equ 0 (
    echo .NET Runtime is installed.
) else (
    echo .NET 6.0 Runtime not found.
    echo Please download and install it from:
    echo https://dotnet.microsoft.com/download/dotnet/6.0
    echo Make sure to install the "Desktop Runtime" version.
    echo.
    echo Press any key when you've installed .NET 6.0...
    pause >nul
)

:: Build the application
echo.
echo Building the application...
dotnet build --configuration Release
if %errorlevel% equ 0 (
    echo Build successful!
) else (
    echo Build failed. Please check the error messages above.
    echo Make sure .NET 6.0 SDK is installed.
    pause
    exit /b 1
)

echo.
echo ====================================
echo           Setup Complete!
echo ====================================
echo.
echo You can now run the application with:
echo   dotnet run
echo.
echo Or use the executable from:
echo   bin\Release\net6.0-windows\YtDlpWrapper.exe
echo.
echo Dependencies required in this folder:
echo   [X] yt-dlp.exe
echo   [X] ffmpeg.exe
echo   [X] .NET 6.0 Runtime
echo.
echo Press any key to exit...
pause >nul 