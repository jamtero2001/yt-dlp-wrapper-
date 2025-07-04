# PowerShell script to convert PNG to ICO
param(
    [string]$PngPath = "your-logo.png",
    [string]$IcoPath = "icon.ico"
)

Write-Host "Converting PNG to ICO..." -ForegroundColor Cyan

try {
    Add-Type -AssemblyName System.Drawing
    
    if (-not (Test-Path $PngPath)) {
        Write-Host "❌ PNG file not found: $PngPath" -ForegroundColor Red
        Write-Host "Please place your PNG file in this folder and update the filename" -ForegroundColor Yellow
        return
    }
    
    # Load the PNG image
    $originalImage = [System.Drawing.Image]::FromFile((Resolve-Path $PngPath))
    Write-Host "✅ Loaded PNG: $PngPath" -ForegroundColor Green
    
    # Create multiple sizes for ICO (16, 32, 48, 64, 128, 256)
    $sizes = @(256, 128, 64, 48, 32, 16)
    $bitmaps = @()
    
    foreach ($size in $sizes) {
        $bitmap = New-Object System.Drawing.Bitmap($size, $size)
        $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
        $graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
        $graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
        
        # Draw resized image
        $graphics.DrawImage($originalImage, 0, 0, $size, $size)
        $graphics.Dispose()
        
        $bitmaps += $bitmap
        Write-Host "✅ Created ${size}x${size} version" -ForegroundColor Green
    }
    
    # Save as ICO (this creates a basic ICO file)
    $bitmaps[0].Save($IcoPath, [System.Drawing.Imaging.ImageFormat]::Icon)
    Write-Host "✅ Created $IcoPath" -ForegroundColor Green
    
    # Cleanup
    $originalImage.Dispose()
    foreach ($bitmap in $bitmaps) {
        $bitmap.Dispose()
    }
    
    Write-Host "🎉 Conversion complete!" -ForegroundColor Green
    Write-Host "📁 Icon saved as: $IcoPath" -ForegroundColor Cyan
    
} catch {
    Write-Host "❌ Error converting PNG to ICO: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "💡 Try using an online converter instead:" -ForegroundColor Yellow
    Write-Host "   - https://favicon.io/favicon-converter/" -ForegroundColor Yellow
    Write-Host "   - https://convertico.com/" -ForegroundColor Yellow
} 