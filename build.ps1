Write-Host "Building yt-dlp Wrapper..." -ForegroundColor Green

# Restore packages
Write-Host "Restoring packages..." -ForegroundColor Yellow
dotnet restore

if ($LASTEXITCODE -ne 0) {
    Write-Host "Package restore failed!" -ForegroundColor Red
    exit 1
}

# Build the project
Write-Host "Building project..." -ForegroundColor Yellow
dotnet build --configuration Release

if ($LASTEXITCODE -eq 0) {
    Write-Host "Build successful!" -ForegroundColor Green
    Write-Host ""
    Write-Host "To run the application:" -ForegroundColor Cyan
    Write-Host "dotnet run --configuration Release" -ForegroundColor White
    Write-Host ""
} else {
    Write-Host "Build failed!" -ForegroundColor Red
    exit 1
} 