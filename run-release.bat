@echo off
echo Starting YT-DLP Wrapper (Release Build)...
echo.
cd "bin\Release\net6.0-windows\win-x64"
if not exist "YtDlpWrapper.exe" (
    echo Release build not found. Building release version...
    cd ..\..\..\..\
    dotnet build --configuration Release
    cd "bin\Release\net6.0-windows\win-x64"
)
start "" "YtDlpWrapper.exe"
echo Application started from: %CD%
echo.
pause 