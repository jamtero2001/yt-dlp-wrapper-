@echo off
title YT-DLP Wrapper - Build Installer
color 0A

echo ================================================================
echo                    YT-DLP Wrapper Installer Builder
echo ================================================================
echo.

:: Check if Inno Setup is installed
set "INNO_PATH="
if exist "C:\Program Files (x86)\Inno Setup 6\ISCC.exe" (
    set "INNO_PATH=C:\Program Files (x86)\Inno Setup 6\ISCC.exe"
) else if exist "C:\Program Files\Inno Setup 6\ISCC.exe" (
    set "INNO_PATH=C:\Program Files\Inno Setup 6\ISCC.exe"
) else (
    echo ERROR: Inno Setup 6 not found!
    echo Please install Inno Setup from: https://jrsoftware.org/isdl.php
    echo.
    pause
    exit /b 1
)

echo ✓ Found Inno Setup at: %INNO_PATH%
echo.

:: Check if application is built
if not exist "bin\Release\net6.0-windows\win-x64\YtDlpWrapper.exe" (
    echo Building application first...
    dotnet build --configuration Release
    if errorlevel 1 (
        echo ERROR: Failed to build application!
        pause
        exit /b 1
    )
    echo ✓ Application built successfully
    echo.
)

:: Check for dependencies
echo Checking dependencies...
if exist "bin\Release\net6.0-windows\win-x64\yt-dlp.exe" (
    echo ✓ yt-dlp.exe found
) else (
    echo ⚠ yt-dlp.exe not found - will be downloaded automatically
)

if exist "bin\Release\net6.0-windows\win-x64\ffmpeg.exe" (
    echo ✓ ffmpeg.exe found
) else (
    echo ⚠ ffmpeg.exe not found - user will need to download manually
    echo   Download from: https://ffmpeg.org/download.html
)

echo.

:: Create output directory
if not exist "installer-output" mkdir installer-output

:: Build the installer
echo Building installer...
"%INNO_PATH%" "YT-DLP-Wrapper-Installer.iss"

if errorlevel 1 (
    echo.
    echo ERROR: Failed to build installer!
    pause
    exit /b 1
)

echo.
echo ================================================================
echo                    INSTALLER BUILD SUCCESSFUL!
echo ================================================================
echo.
echo Installer created: installer-output\YT-DLP-Wrapper-Setup-v1.0.4.exe
echo.
echo The installer includes:
echo   ✓ YT-DLP Wrapper application
echo   ✓ Desktop shortcut creation
echo   ✓ Start menu entry
echo   ✓ Proper uninstaller
echo   ✓ Automatic dependency checking
echo   ✓ Professional Windows integration
echo.
echo You can now distribute the installer file to users!
echo.

:: Ask if user wants to test the installer
set /p "test=Do you want to test the installer now? (y/n): "
if /i "%test%"=="y" (
    echo.
    echo Starting installer...
    start "" "installer-output\YT-DLP-Wrapper-Setup-v1.0.4.exe"
)

pause 