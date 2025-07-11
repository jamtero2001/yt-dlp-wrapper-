@echo off
echo Starting YT-DLP Wrapper (Debug Build)...
echo.
cd "bin\Debug\net6.0-windows\win-x64"
start "" "YtDlpWrapper.exe"
echo Application started from: %CD%
echo.
pause 