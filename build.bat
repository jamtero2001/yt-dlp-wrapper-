@echo off
echo Building yt-dlp Wrapper...

REM Restore packages
dotnet restore

REM Build the project
dotnet build --configuration Release

if %ERRORLEVEL% EQU 0 (
    echo Build successful!
    echo.
    echo To run the application:
    echo dotnet run --configuration Release
    echo.
    pause
) else (
    echo Build failed!
    pause
) 